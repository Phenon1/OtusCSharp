using OtusCSharpModels;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

                if (message.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
                {
                    int index = message.IndexOf('{');

                    string command = message[..index];
                    string json = message[index..];

                    UserProfile profile =
                        JsonSerializer.Deserialize<UserProfile>(json)!;

                    byte[] commandBytes =
                        Encoding.UTF8.GetBytes(command);

                    byte[] valueBytes =
                        profile.SerializeToBinary();

                    byte[] data =
                        new byte[commandBytes.Length + valueBytes.Length];

                    Buffer.BlockCopy(
                        commandBytes,
                        0,
                        data,
                        0,
                        commandBytes.Length);

                    Buffer.BlockCopy(
                        valueBytes,
                        0,
                        data,
                        commandBytes.Length,
                        valueBytes.Length);


                    byte[] length = BitConverter.GetBytes(data.Length);
                    await client.SendAsync(length, SocketFlags.None);
                    await client.SendAsync(data, SocketFlags.None);
                }
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);

                    byte[] length = BitConverter.GetBytes(data.Length);
                    await client.SendAsync(length, SocketFlags.None);
                    await client.SendAsync(data, SocketFlags.None);
                }
            }
        }

        private async Task ReceiveAsync(Socket client)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (true)
                {
                    byte[] header = new byte[4];
                    bool closed = await ReadExactAsync(client, header, 4);
                    int length = BitConverter.ToInt32(header, 0);

                    if (length == 0 || closed)
                        break;


                    await ReadExactAsync(client, buffer, length);
                   

                    try
                    {
                        var profile =
                            UserProfile.DeserializeFromBinary(
                                buffer.AsSpan(0, length));

                        Console.WriteLine(
                            $"User: {profile.Id} {profile.Username}");
                    }
                    catch
                    {
                        string response =
                            Encoding.UTF8.GetString(buffer, 0, length);

                        Console.WriteLine($"Ответ сервера: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

       private static async ValueTask<bool> ReadExactAsync(
       Socket socket,
       byte[] buffer,
       int size)
        {
            int total = 0;

            while (total < size)
            {
                int received =
                    await socket.ReceiveAsync(
                        buffer.AsMemory(total, size - total),
                        SocketFlags.None);

                if (received == 0)
                    return true;

                total += received;
            }

            return false;
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