using System.Net.Sockets;

namespace PhenonExtensions
{
    public static class TcpSocketExtensions
    {
        public static async Task SendExactAsync(
            this Socket socket,
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            int total = 0;

            while (total < data.Length)
            {
                int sent =
                    await socket.SendAsync(
                        data.AsMemory(total),
                        SocketFlags.None,
                        cancellationToken);

                if (sent == 0)
                    throw new Exception(
                        "Socket disconnected");

                total += sent;
            }
        }

        public static async ValueTask<bool> ReadExactAsync(
            this Socket socket,
            byte[] buffer,
            int size,
            CancellationToken cancellationToken = default)
        {
            int total = 0;

            while (total < size)
            {
                int received =
                    await socket.ReceiveAsync(
                        buffer.AsMemory(total, size - total),
                        SocketFlags.None,
                        cancellationToken);

                if (received == 0)
                    return true;

                total += received;
            }

            return false;
        }

        public static async Task SendPacketWithLenAsync(
            this Socket socket,
            byte[] payload,
            CancellationToken cancellationToken = default)
        {
            byte[] length =
                BitConverter.GetBytes(payload.Length);

            await socket.SendExactAsync(length, cancellationToken);
            await socket.SendExactAsync(payload, cancellationToken);
        }

        public static async Task<byte[]> ReceivePacketWithLenAsync(
            this Socket socket,
            CancellationToken cancellationToken = default)
        {
            byte[] header = new byte[4];

            bool disconnected = await socket.ReadExactAsync(header, 4, cancellationToken);

            if (disconnected)
                return [];

            int length = BitConverter.ToInt32(header, 0);

            byte[] payload = new byte[length];

            disconnected = await socket.ReadExactAsync(payload, length, cancellationToken);

            if (disconnected)
                return [];

            return payload;
        }
    }
}

