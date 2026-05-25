using System.Net.Sockets;

namespace PhenonExtensions
{
    public static class TcpSocketExtensions
    {
        public static async Task SendExactAsync(
            this Socket socket,
            byte[] data)
        {
            int total = 0;

            while (total < data.Length)
            {
                int sent =
                    await socket.SendAsync(
                        data.AsMemory(total),
                        SocketFlags.None);

                if (sent == 0)
                    throw new Exception(
                        "Socket disconnected");

                total += sent;
            }
        }

        public static async ValueTask<bool> ReadExactAsync(
            this Socket socket,
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

        public static async Task SendPacketWithLenAsync(
            this Socket socket,
            byte[] payload)
        {
            byte[] length =
                BitConverter.GetBytes(payload.Length);

            await socket.SendExactAsync(length);
            await socket.SendExactAsync(payload);
        }

        public static async Task<byte[]> ReceivePacketWithLenAsync(
            this Socket socket)
        {
            byte[] header = new byte[4];

            bool disconnected = await socket.ReadExactAsync(header, 4);

            if (disconnected)
                return [];

            int length = BitConverter.ToInt32(header, 0);

            byte[] payload = new byte[length];

            disconnected = await socket.ReadExactAsync(payload, length);

            if (disconnected)
                return [];

            return payload;
        }
    }
}