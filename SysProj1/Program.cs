public class Program
{
    private static void Main(string[] args)
    {
        Handler handler = new();
        Server server = new(handler.Handle);
        server.Start("5050");
        Console.WriteLine("Press any button to quit.");
        Console.ReadLine();
        server.Stop();
    }
}
