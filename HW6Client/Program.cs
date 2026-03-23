using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HW6Client
{
    public class TcpClientApp
    {
        public async Task StartAsync(string host, int port)
        {
            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await client.ConnectAsync(IPAddress.Parse(host), port);
            Console.WriteLine("Подключено к серверу");

            _ = ReceiveAsync(client);

            while (true)
            {
                string? message = Console.ReadLine();
                if (string.IsNullOrEmpty(message))
                    continue;

                byte[] data = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(data, SocketFlags.None);
            }
        }

        private async Task ReceiveAsync(Socket client)
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0)
                        break;

                    string response = Encoding.UTF8.GetString(buffer, 0, received);
                    Console.WriteLine($"Ответ сервера: {response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            TcpClientApp client = new TcpClientApp();
            await client.StartAsync("127.0.0.1", 8888);
        }
    }
}