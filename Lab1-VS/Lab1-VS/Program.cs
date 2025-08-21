using System.Collections.Concurrent;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

IResult ReturnResponse(string message, string returnType)
{
    Console.WriteLine(message);
    return returnType switch
    {
        "PROBLEM" => Results.Problem(message),
        "NOT_FOUND" => Results.NotFound(message),
        "OK" => Results.Ok(message),
        _ => Results.BadRequest("Unknown return type")
    };
}

ConcurrentDictionary<string, string> cache = new();
object __lockobj = new();

app.MapGet("/", () => Results.NotFound("Please provide an input file"));
app.MapGet("/{inputFileName}", (string inputFileName) =>
{
    if (inputFileName.Trim() == "")
        return ReturnResponse("File name can't be empty", "PROBLEM");
    if (!File.Exists(inputFileName))
        return ReturnResponse($"File {inputFileName} doesn't exist", "NOT_FOUND");
    if (cache.TryGetValue(inputFileName, out string? cachedHash))
        return ReturnResponse($"Cached hash of {inputFileName}: {cachedHash}", "OK");

    var thread = new Thread(() =>
    {
        lock (__lockobj) // nije neophodno jer Thread automatski resava race conditions
        {
            byte[] inputBytes = File.ReadAllBytes(inputFileName);
            // if (inputBytes.Length == 0) // ne radi zbog toga sto ne mozemo return u threadu
            //     return ReturnResponse("File name can't be empty", "PROBLEM");
            byte[] inputBytesHashed = SHA256.HashData(inputBytes);
            string hashedFileText = Convert.ToHexString(inputBytesHashed);
            cache[inputFileName] = hashedFileText;
        }
    });
    thread.Start();
    thread.Join();
    return ReturnResponse($"Hashed file: {inputFileName} =>\n{cache[inputFileName]}", "OK");
    // string outputFileName = inputFileName + "-hashed";
    // File.WriteAllText(outputFileName, hashedFileText);
});

// Ovo je mozda bolja implementacija, jer nam dozvoljava da vracamo response
// unutar celog taska. Jedini razlog zbog cega ovo nije glavna implementacija
// je zbog toga sto ste rekli u zadatku da koristimo threadove

app.MapGet("/tasks/{inputFileName}", async (string inputFileName) =>
{
    return await Task.Run(() =>
    {
        if (inputFileName.Trim() == "")
            return ReturnResponse("File name can't be empty", "PROBLEM");
        if (!File.Exists(inputFileName))
            return ReturnResponse($"File {inputFileName} doesn't exist", "NOT_FOUND");
        if (cache.TryGetValue(inputFileName, out string? cachedHash))
            return ReturnResponse($"Cached hash of {inputFileName}: {cachedHash}", "OK");

        lock (__lockobj)
        {
            byte[] inputBytes = File.ReadAllBytes(inputFileName);
            if (inputBytes.Length == 0)
                return ReturnResponse("File name can't be empty", "PROBLEM");
            byte[] inputBytesHashed = SHA256.HashData(inputBytes);
            string hashedFileText = Convert.ToHexString(inputBytesHashed);
            cache[inputFileName] = hashedFileText;
        }
        return ReturnResponse($"Hashed file: {inputFileName} =>\n{cache[inputFileName]}", "OK");
        // string outputFileName = inputFileName + "-hashed";
        // File.WriteAllText(outputFileName, hashedFileText);
    });
});

app.Run();
