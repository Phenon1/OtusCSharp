using HW14TCPServer;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        var server = new HighPerformanceTcpServer(7777);

        // Запуск сервера в фоновой задаче
        Task serverTask = server.StartAsync();

        Console.WriteLine("Нажмите Enter для остановки сервера...");
        Console.ReadLine();

        server.Stop();
        await serverTask;

        Console.WriteLine("Сервер остановлен");
    }
}

class HighPerformanceTcpServer
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts;
    private static SemaphoreSlim _cpuWorkerSemaphore = new SemaphoreSlim(Environment.ProcessorCount);

    public HighPerformanceTcpServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine($"Сервер запущен на порту {((IPEndPoint)_listener.LocalEndpoint).Port}");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                //Console.WriteLine($"Клиент подключен: {client.Client.RemoteEndPoint}");

                // Запуск обработки клиента без ожидания
                _ = HandleClientAsync(client, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Сервер останавливается...");
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using NetworkStream stream = client.GetStream();

        try
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                // Аренда буфера из пула
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);

                try
                {
                    // Асинхронное чтение с ValueTask
                    ValueTask<int> readTask = stream.ReadAsync(new Memory<byte>(buffer), cancellationToken);
                    int bytesRead = await readTask;

                    // Клиент отключился
                    if (bytesRead == 0)
                    {
                        //Console.WriteLine($"Клиент отключился: {client.Client.RemoteEndPoint}");
                        break;
                    }

                    await _cpuWorkerSemaphore.WaitAsync();
                    try
                    {
                        ReadOnlySpan<char> spanMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).AsSpan();

                        HandleMessage(spanMessage);
                        //int endIndex = message.IndexOf("\r\n");
                        //if(endIndex != message.Length)
                        {

                        }

                        
                       
                        
                    }
                    finally
                    {
                        _cpuWorkerSemaphore.Release();
                    }

                    // Подготовка ответа
                    string response = $"pong";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);

                    // Асинхронная отправка с ValueTask
                    ValueTask writeTask = stream.WriteAsync(new ReadOnlyMemory<byte>(responseData), cancellationToken);
                    await writeTask;
                    await stream.FlushAsync(cancellationToken);

                    //Console.WriteLine($"Ответ отправлен клиенту: {client.Client.RemoteEndPoint}");
                }
                finally
                {
                    // Возврат буфера в пул
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке клиента: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private void HandleMessage(ReadOnlySpan<char> span)
    {
        span = TrimStart(span);
        int space = span.IndexOf(' ');

        if (space == -1)
            throw new ComandException();

        ReadOnlySpan<char> command = span[..(space)];



        if (space + 1 > span.Length)
            throw new ComandException();

        span = span[(space)..];
        span = TrimStart(span);
        space = span.IndexOf(' ');

        if (span.Length == 0)
            throw new ComandException();
        else if (space == -1)
            space = span.Length;



        ReadOnlySpan<char> key = span[..(space)];

        ReadOnlySpan<char> value = new ReadOnlySpan<char>();

        if (space < span.Length)
        {
            span = span[(space)..];
            span = TrimStart(span);
            value = span;
        }
    }

    private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
    {
        while (span.Length > 0)
        {
            if (span[0] == ' ')
                span = span[1..];
            else return span;
        }
        return span;
    }

    public class ComandException : Exception { }

    public void Stop()
        {
            _cts.Cancel();
        }
    }