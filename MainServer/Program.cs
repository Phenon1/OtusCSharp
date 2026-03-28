namespace MainServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpServer server = new TcpServer(new OtusCSharpHW3.SimpleStore());
            _ = server.StartAsync(new byte[] { 127, 0, 0, 1 }, 8888);
            Console.ReadLine();
        }
    }
}
