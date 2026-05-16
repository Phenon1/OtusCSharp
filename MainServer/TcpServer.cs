using Microsoft.Extensions.Configuration;
using OtusCSharpModels;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MainServer
{
    public class TcpServer
    {
        private SimpleStore _store;
        int _maxSizeMessageByte;
        int _maxCountConnect;
        SemaphoreSlim _semaphoreConnectCount;

        public TcpServer(SimpleStore store)
        {
            _store = store;
            IConfiguration config = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();

            _maxSizeMessageByte = config.GetValue<int>("IncomeMessageSettings:SizeByte");
            _maxCountConnect = config.GetValue<int>("IncomeMessageSettings:MaxCountConnect");
            _semaphoreConnectCount = new SemaphoreSlim(_maxCountConnect);

        }

        private enum AvCommands
        {
            GET,SET,DELETE
        }

      
        const string OK = "OK";
        const string Nil = "(nil)";
        const string ErrorUnknownCommand = "-ERR Unknown command";
        const string ErrorTooLarge = "-ERR Command too long. Max {0} byte allowed.";

        public async Task StartAsync(byte[] bAddress, int port)
        {

            IPAddress address = new IPAddress(bAddress);
            IPEndPoint ipPoint = new IPEndPoint(address, port);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen();


            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (true)
            {
                
                await _semaphoreConnectCount.WaitAsync();

                Socket client = await socket.AcceptAsync();
                _ = Task.Run(() => ProcessClientAsync(client));
                // получаем адрес клиента
                Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");
                
               
            }

        }

        private async Task SendMessageWithEndLineAsync(string message, Socket socket)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(message+ "\r\n"));
        }
        private async Task ProcessClientAsync(Socket client)
        {
            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(_maxSizeMessageByte);

            string clientAddress = client.RemoteEndPoint?.ToString() ?? "unknown";

            try
            {
                while (true)
                {
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    var stopwatch = Stopwatch.StartNew();

                    if (received == 0) // клиент закрыл соединение
                        break;

                    if (received >= _maxSizeMessageByte)
                    {
                        string errorMessage = string.Format(ErrorTooLarge, _maxSizeMessageByte);
                        using (Activity? activity = GetErrorReceiveMessageActivity(clientAddress, errorMessage))
                        {
                            await SendMessageWithEndLineAsync(errorMessage, client);
                            break;
                        }
                    }
                       


                    ReadOnlySpan<char> span = Encoding.UTF8.GetChars(buffer, 0, received);
                    
                    CommandKeyValue command;
                    string commandName;
                    using (Activity? activity = GetReceiveMessageActivity(clientAddress))
                    {
                        command = CommandParser.Parse(span);
                        command.Print();
                        commandName = command.Command.ToString();
                        activity?.SetTag("command.name", commandName);
                    }

                    switch (commandName.ToUpper())
                    {
                        case nameof(AvCommands.SET):
                            _store.Set(command.Key.ToString(), JsonSerializer.Deserialize<UserProfile>(command.Value));
                            await SendMessageWithEndLineAsync(OK, client);
                            break;

                        case nameof(AvCommands.GET):
                            UserProfile? val = _store.Get(command.Key.ToString());

                            if (val == null)
                                await SendMessageWithEndLineAsync(Nil,client);
                            else
                                await client.SendAsync(JsonSerializer.SerializeToUtf8Bytes(val));
                            break;

                        case nameof(AvCommands.DELETE):
                            _store.Delete(command.Key.ToString());
                            await SendMessageWithEndLineAsync(OK, client);
                            break;

                        default:
                            await SendMessageWithEndLineAsync(ErrorUnknownCommand, client);
                            break;
                    }

                    stopwatch.Stop();

                    var tags = new KeyValuePair<string, object?>("command", commandName);
                    Telemetry.CommandCounter.Add(1, tags);
                    Telemetry.CommandDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
                }
            }
            catch (SocketException ex)
            {
                string errorMessage = $"Socket error: {ex.Message}";
                using (Activity? activity = GetErrorReceiveMessageActivity(clientAddress, errorMessage))
                {
                    await SendMessageWithEndLineAsync(errorMessage, client);
                }

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error: {ex}";
                using (Activity? activity = GetErrorReceiveMessageActivity(clientAddress, errorMessage))
                {
                    await SendMessageWithEndLineAsync(errorMessage, client);
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
            }
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
    }
}
