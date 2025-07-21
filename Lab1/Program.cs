using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.NotFound("Please provide an input file"));

app.MapGet("/{inputFileName}", (string inputFileName) =>
{
    if (inputFileName.Trim() == "")
    {
        return Results.Problem("File name can't be empty");
    }
    if (!File.Exists(inputFileName))
    {
        return Results.NotFound($"File {inputFileName} doesn't exist");
    }

    byte[] inputBytes = File.ReadAllBytes(inputFileName);
    if (inputBytes.Length == 0)
    {
        return Results.Problem("File can't be empty");
    }
    byte[] inputBytesHashed = SHA256.HashData(inputBytes);
    string hashedFileText = Convert.ToHexString(inputBytesHashed);

    string outputFileName = inputFileName + "-hashed";
    File.WriteAllText(outputFileName, hashedFileText);

    return Results.Ok($"Hashed file: {inputFileName} => {outputFileName}");
});

app.Run();
