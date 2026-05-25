using NBomber.Contracts.Cluster;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PhenonExtensions;

namespace NBomberTest
{
    public class TcpBombClient : IDisposable
    {
        private Socket _client = null!;
        private readonly SemaphoreSlim _lock = new(1, 1);


        public async Task ConnectAsync(string host, int port, CancellationToken cancellation)
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await _client.ConnectAsync(IPAddress.Parse(host), port);
           // Console.WriteLine("Подключено к серверу");

        }

        public async Task SetAsync(string message, byte[] value)
        {

            await _lock.WaitAsync();
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("SET " + message +" ");
                byte[] combined = new byte[data.Length + value.Length];
                Buffer.BlockCopy(data, 0, combined, 0, data.Length);
                Buffer.BlockCopy(value, 0, combined, data.Length, value.Length);


                byte[] length = BitConverter.GetBytes(combined.Length);

                await _client.SendPacketWithLenAsync(combined);

                await ReceiveAsync(_client);
            }
            finally
            {
                _lock.Release(); 
            }
        }

        public async Task<byte[]> GetAsync(string key)
        {
            await _lock.WaitAsync();
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("GET " + key);

                await _client.SendPacketWithLenAsync(data);
                return await ReceiveAsync(_client);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<byte[]> ReceiveAsync(Socket client)
        {
            byte[] buffer = new byte[1024];
            try
            {
                return await client.ReceivePacketWithLenAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return buffer;
            }
        }

        public void Dispose()
        {
            _client?.Shutdown(SocketShutdown.Send);
            _client?.Close();
            _client?.Dispose();
        }

    }
}
