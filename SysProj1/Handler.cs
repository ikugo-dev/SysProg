using System.Net;
using System.Security.Cryptography;
using System.Collections.Concurrent;

public class Handler
{
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly object _lockObj = new();

    public void Handle(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET")
        {
            WriteResponse(context.Response, 400, "Only GET requests are supported.");
            return;
        }

        string path = context.Request.Url!.AbsolutePath;
        if (!path.StartsWith("/api/"))
        {
            WriteResponse(context.Response, 400, "Api calls start with /api/{inputFileName}");
            return;
        }

        string inputFileName = path.Remove(0, 5);
        if (string.IsNullOrWhiteSpace(inputFileName))
        {
            WriteResponse(context.Response, 400, "File name can't be empty.");
            return;
        }

        string inputFilePath = Directory.GetFiles(
            Directory.GetCurrentDirectory(),
            inputFileName,
            SearchOption.AllDirectories
        ).FirstOrDefault("");
        if (inputFilePath == "")
        {
            WriteResponse(context.Response, 404, $"File {inputFileName} doesn't exist.");
            return;
        }
        // Console.WriteLine($"Found {inputFileName} at: {inputFilePath}");

        if (_cache.TryGetValue(inputFileName, out string? cachedHash))
        {
            WriteResponse(context.Response, 200, $"Cached hash: {inputFileName} => {cachedHash}");
            return;
        }

        string hashedFileText;
        lock (_lockObj)
        {
            byte[] inputBytes = File.ReadAllBytes(inputFilePath);
            if (inputBytes.Length == 0)
            {
                WriteResponse(context.Response, 404, "File can't be empty");
                return;
            }
            byte[] inputBytesHashed = SHA256.HashData(inputBytes);
            hashedFileText = Convert.ToHexString(inputBytesHashed);
            _cache[inputFileName] = hashedFileText;
        }

        WriteResponse(context.Response, 200, $"Hashed file: {inputFileName} => {hashedFileText}");
    }

    private static void WriteResponse(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        using var writer = new StreamWriter(response.OutputStream);
        writer.Write(message);
        writer.Flush();
        response.Close();
        Console.WriteLine($"{DateTime.Now.TimeOfDay}: {statusCode} {message}");
    }
}
