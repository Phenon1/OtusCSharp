using OtusCSharpModels;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MainServer
{
    public class TcpServer
    {
        private SimpleStore _store;
        public TcpServer(SimpleStore store)
        {
            _store = store;
        }

        private enum AvCommands
        {
            GET,SET,DELETE
        }

        const string OK = "OK";
        const string Nil = "(nil)";
        const string ErrorUnknownCommand = "-ERR Unknown command";

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
                // получаем входящее подключение
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
            byte[] buffer = pool.Rent(1024);

            try
            {
                while (true)
                {
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0) // клиент закрыл соединение
                        break;
                    
                    ReadOnlySpan<char> span = Encoding.UTF8.GetChars(buffer, 0, received);
                    
                    CommandKeyValue command;

                    command = CommandParser.Parse(span);
                    command.Print();

                    switch (command.Command.ToString().ToUpper())
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

                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            finally
            {
                Console.WriteLine($"Клиент отключился: {client.RemoteEndPoint}");
                try { client.Shutdown(SocketShutdown.Both); } catch { }
                client.Close();
                client.Dispose();
                pool.Return(buffer);
            }
        }
    }
}
