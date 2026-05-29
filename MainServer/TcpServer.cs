using Microsoft.Extensions.Configuration;
using OtusCSharpModels;
using PhenonExtensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MainServer
{
    public class TcpServer : IDisposable
    {
        private SimpleStore _store;
        private readonly int _maxSizeMessageByte;
        private readonly int _maxCountConnect;
        private readonly SemaphoreSlim _semaphoreConnectCount;

        public TcpServer(SimpleStore store, IConfiguration config)
        {
            _store = store;
            _maxSizeMessageByte = config.GetValue<int>("IncomeMessageSettings:SizeByte");
            _maxCountConnect = config.GetValue<int>("IncomeMessageSettings:MaxCountConnect");
            _semaphoreConnectCount = new SemaphoreSlim(_maxCountConnect);

        }

        private const string CommandGet = "GET";
        private const string CommandSet = "SET";
        private const string CommandDelete = "DELETE";
        private const string CommandUnknown = "UNKNOWN";

        private static ReadOnlySpan<byte> GetCommandBytes => "GET"u8;
        private static ReadOnlySpan<byte> SetCommandBytes => "SET"u8;
        private static ReadOnlySpan<byte> DeleteCommandBytes => "DELETE"u8;

        const string OK = "OK";
        const string Nil = "(nil)";
        const string ErrorUnknownCommand = "-ERR Unknown command";
        const string ErrorTooLarge = "-ERR Command too long. Max {0} byte allowed.";

        public async Task StartAsync(byte[] bAddress, int port, CancellationToken cancellationToken = default)
        {

            IPAddress address = new IPAddress(bAddress);
            IPEndPoint ipPoint = new IPEndPoint(address, port);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen();


            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _semaphoreConnectCount.WaitAsync(cancellationToken);

                    Socket client;

                    try
                    {
                        client = await socket.AcceptAsync(cancellationToken);
                    }
                    catch
                    {
                        _semaphoreConnectCount.Release();
                        throw;
                    }

                    _ = ProcessClientAsync(client, cancellationToken);
                    // получаем адрес клиента
                    Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }

        }

        private async Task SendMessageWithEndLineAsync(string message, Socket socket, CancellationToken cancellationToken)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message + "\r\n");
            await socket.SendPacketWithLenAsync(payload, cancellationToken);
        }

        private async Task ProcessClientAsync(Socket client, CancellationToken cancellationToken)
        {
            var pool = ArrayPool<byte>.Shared;

            byte[] buffer = pool.Rent(_maxSizeMessageByte);
            byte[] headerBuffer = pool.Rent(4);

            string clientAddress = client.RemoteEndPoint?.ToString() ?? "unknown";

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    bool disconnected = await client.ReadExactAsync(headerBuffer, 4, cancellationToken);

                    if (disconnected) // клиент закрыл соединение
                        break;

                    int messageLength =
                        BitConverter.ToInt32(headerBuffer, 0);

                    if (messageLength > _maxSizeMessageByte)
                    {
                        string errorMessage = string.Format(ErrorTooLarge, _maxSizeMessageByte);
                        using (Activity? activity = GetErrorReceiveMessageActivity(clientAddress, errorMessage))
                        {
                            await SendMessageWithEndLineAsync(errorMessage, client, cancellationToken);
                            break;
                        }
                    }

                    disconnected =
                        await client.ReadExactAsync(buffer, messageLength, cancellationToken);

                    if (disconnected)
                        break;

                    long start = Stopwatch.GetTimestamp();
                    ReadOnlySpan<byte> span = buffer.AsSpan(0, messageLength);

                    CommandKeyValue command;
                    string commandName;
                    using (Activity? activity = GetReceiveMessageActivity(clientAddress))
                    {
                        command = CommandParser.Parse(span);
                        //command.Print();
                        commandName = GetCommandName(command.Command);
                        activity?.SetTag("command.name", commandName);
                    }

                    switch (commandName)
                    {
                        case CommandSet:
                            _store.Set(Encoding.UTF8.GetString(command.Key), UserProfile.DeserializeFromBinary(command.Value));
                            await SendMessageWithEndLineAsync(OK, client, cancellationToken);
                            break;

                        case CommandGet:
                            UserProfile? val = _store.Get(Encoding.UTF8.GetString(command.Key));

                            if (val == null)
                                await SendMessageWithEndLineAsync(Nil, client, cancellationToken);
                            else
                                await client.SendPacketWithLenAsync(val.SerializeToBinary(), cancellationToken);
                            break;

                        case CommandDelete:
                            _store.Delete(Encoding.UTF8.GetString(command.Key));
                            await SendMessageWithEndLineAsync(OK, client, cancellationToken);
                            break;

                        default:
                            await SendMessageWithEndLineAsync(ErrorUnknownCommand, client, cancellationToken);
                            break;
                    }

                    var tags = new KeyValuePair<string, object?>("command", commandName);
                    Telemetry.CommandCounter.Add(1, tags);
                    Telemetry.CommandDurationHistogram.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (SocketException ex)
            {
                string errorMessage = $"Socket error: {ex.Message}";
                using (Activity? activity = GetErrorReceiveMessageActivity(clientAddress, errorMessage))
                {
                    await SendMessageWithEndLineAsync(errorMessage, client, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error: {ex}";
                using (Activity? activity = GetErrorReceiveMessageActivity(clientAddress, errorMessage))
                {
                    await SendMessageWithEndLineAsync(errorMessage, client, cancellationToken);
                }
            }
            finally
            {
                _semaphoreConnectCount.Release();
                Console.WriteLine($"Клиент отключился: {client.RemoteEndPoint}");
                try { client.Shutdown(SocketShutdown.Both); } catch { }
                client.Close();
                client.Dispose();
                pool.Return(buffer);
                pool.Return(headerBuffer);
            }
        }

        private static string GetCommandName(ReadOnlySpan<byte> command)
        {
            if (EqualsAsciiIgnoreCase(command, SetCommandBytes))
                return CommandSet;

            if (EqualsAsciiIgnoreCase(command, GetCommandBytes))
                return CommandGet;

            if (EqualsAsciiIgnoreCase(command, DeleteCommandBytes))
                return CommandDelete;

            return CommandUnknown;
        }

        private static bool EqualsAsciiIgnoreCase(ReadOnlySpan<byte> command, ReadOnlySpan<byte> expected)
        {
            if (command.Length != expected.Length)
                return false;

            for (int i = 0; i < command.Length; i++)
            {
                byte value = command[i];

                if (value >= (byte)'a' && value <= (byte)'z')
                    value -= (byte)('a' - 'A');

                if (value != expected[i])
                    return false;
            }

            return true;
        }

        private Activity? GetReceiveMessageActivity(string remoteEndPoint)
        {
            var initialTags = new ActivityTagsCollection
            {
                { "client.endpoint", remoteEndPoint }
            };

            return Telemetry.Source.StartActivity(
                 name: "ReceiveMessage",
                 kind: ActivityKind.Server,
                 tags: initialTags
             );
        }

        private Activity? GetErrorReceiveMessageActivity(string remoteEndPoint, string error)
        {
            Activity? activity = GetReceiveMessageActivity(remoteEndPoint);
            activity?.SetTag("status", "error");
            activity?.SetTag("error.message", error);
            return activity;
        }

        public void Dispose()
        {
            _store.Dispose();
            _semaphoreConnectCount.Dispose();
        }
    }
}
