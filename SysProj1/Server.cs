using System.Net;

public class Server
{
    private bool _isRunning;
    private readonly HttpListener _listener = new();
    private Thread? _listenerThread;
    private readonly Action<HttpListenerContext> _handleRequest;

    public Server(Action<HttpListenerContext> processRequest) => _handleRequest = processRequest;

    public void Start(string port = "5050")
    {
        if (_isRunning) return;

        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _isRunning = true;
        _listenerThread = new Thread(ListenLoop);
        _listenerThread.Start();
        Console.WriteLine($"Server started on port {port}");
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _listener.Stop();
        _listenerThread?.Join();
        Console.WriteLine("Server stopped");
    }

    private void ListenLoop()
    {
        ThreadPool.SetMaxThreads(10, 10);
        try
        {
            while (_isRunning)
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => _handleRequest(context));
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Listen loop has stopped");
        }
    }
}

