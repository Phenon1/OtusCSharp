using Microsoft.Extensions.Configuration;
using OtusCSharpModels;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HW6Server
{
    public class TcpServer
    {
        int maxSizeMessageByte;
        uint maxCountConnect;
        public async Task StartAsync(byte[] bAddress,int port)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            maxSizeMessageByte = config.GetValue<int>("IncomeMessageSettings:SizeByte");
            maxCountConnect = config.GetValue<uint>("IncomeMessageSettings:MaxCountConnect");

            IPAddress address = new IPAddress(bAddress);
            IPEndPoint ipPoint = new IPEndPoint(address, port);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen();
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while ( true )
            {
                // получаем входящее подключение
                Socket client = await socket.AcceptAsync();
                await ProcessClientAsync(client);
                // получаем адрес клиента
                Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");
            }    
            
        }
        private async Task ProcessClientAsync(Socket client)
        {
            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(maxSizeMessageByte);

            try
            {
                while ( true )
                {
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    
                    if ( received == 0 ) // клиент закрыл соединение
                        break;

                    ReadOnlySpan<char> span = Encoding.UTF8.GetString(buffer, 0, received);
                    CommandParser.Parse(span).Print();


                }
            }
            catch ( SocketException ex )
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            catch ( Exception ex )
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

    internal class Program
    {
        static void Main(string[] args)
        {
            TcpServer server = new TcpServer();
            _ = server.StartAsync(new byte[] { 127, 0, 0, 1 }, 8888);


            Console.ReadLine();
        }
    }
}