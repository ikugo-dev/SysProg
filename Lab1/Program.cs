using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.NotFound("Please provide an input file"));

IResult ReturnResponse(string message, string returnType)
{
    Console.WriteLine(message);
    if (returnType == "PROBLEM") return Results.Problem(message);
    if (returnType == "NOT_FOUND") return Results.NotFound(message);
    if (returnType == "OK") return Results.Ok(message);
    return Results.BadRequest("Unknown return type");
}

app.MapGet("/{inputFileName}", (string inputFileName) =>
{
    if (inputFileName.Trim() == "")
        return ReturnResponse("File name can't be empty", "PROBLEM");
    if (!File.Exists(inputFileName))
        return ReturnResponse($"File {inputFileName} doesn't exist", "NOT_FOUND");

    byte[] inputBytes = File.ReadAllBytes(inputFileName);
    if (inputBytes.Length == 0)
        return ReturnResponse("File name can't be empty", "PROBLEM");

    byte[] inputBytesHashed = SHA256.HashData(inputBytes);
    string hashedFileText = Convert.ToHexString(inputBytesHashed);

    string outputFileName = inputFileName + "-hashed";
    File.WriteAllText(outputFileName, hashedFileText);

    return ReturnResponse($"Hashed file: {inputFileName} => {outputFileName}", "OK");
});

app.Run();
