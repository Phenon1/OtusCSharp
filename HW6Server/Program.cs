using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Buffers;
using OtusCSharpHW3;

namespace HW6Server
{
    public class TcpServer
    {
        public async void StartAcync(byte[] bAddress,int port)
        {
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
                ProcessClientAsync(client);
                // получаем адрес клиента
                Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");
            }    
            
        }
        private async void ProcessClientAsync(Socket client)
        {
            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(512);

            try
            {
                Memory <char>  mem = new Memory<char>(buffer);
                byte[] receive = new byte[1];
                ushort count=0;
                char symb;

                while ( true )
                {
                    int received = await client.ReceiveAsync(receive, SocketFlags.None);
                    if ( received == 0 ) // клиент закрыл соединение
                        break;

                    symb = (char)receive[0];

                    if (symb == '\n')
                    {

                        CommandParser.Parse(mem.Span[..--count]).Print();
                        count=0;
                    }
                    else    
                        mem.Span[count++] = symb;
                    
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
            server.StartAcync(new byte[] { 127, 0, 0, 1 }, 8888);
            Console.ReadLine();
        }
    }
}