public class Program
{
    private static void Main(string[] args)
    {
        Handler handler = new(true, true);
        Server server = new(handler.Handle);
        const string port = "5050";
        server.Start(port);
        Console.WriteLine($"API on: http://localhost:{port}/api/");
        Console.WriteLine("Press [Enter] to quit.");
        Console.ReadLine();
        server.Stop();
    }
}
