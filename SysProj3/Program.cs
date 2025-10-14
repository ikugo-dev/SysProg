using Newtonsoft.Json;
using SysProj3;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseFileServer(); // iskomentarisite liniju ako zelite cist api bez index.html

Console.Write("Enter your GitHub API key: ");
string? githubToken = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(githubToken))
{
    Console.WriteLine("No API key entered. Exiting...");
    return;
}

app.MapGet("/api", async (string topic, int pageLimit, int perPage) =>
{
    if (string.IsNullOrWhiteSpace(topic))
        return Results.BadRequest(new { error = "Missing topic parameter" });

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.mercy-preview+json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SysProj3");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    //PAT token used only for higher request limit

    List<string> urls = await Helper.GetLinks(client, topic, pageLimit, perPage);
    var cts = new CancellationTokenSource();

    var observable = urls
        .ToObservable()
        .Select((url) => Observable.FromAsync(async () =>
            {
                cts.Token.ThrowIfCancellationRequested();
                var newRepos = new List<Repo>();
                Console.WriteLine($"Fetching {url} on thread {Thread.CurrentThread.ManagedThreadId}");

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                //Console.WriteLine(response.Headers);
                var content = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(content);
                var apiData = JsonConvert.DeserializeObject<dynamic>(content);
                var repos = apiData!.items;
                var numberOfRepos = apiData!.total_count;
                //Console.WriteLine($"Number of repos on thread: {Thread.CurrentThread.ManagedThreadId}: {numberOfRepos} \n");

                foreach (var repo in repos)
                {
                    var newRepo = new Repo
                    {
                        Name = repo.name,
                        Stars = repo.stargazers_count,
                        Size = repo.size, //change to human readable when displaying
                        Forks = repo.forks_count
                    };
                    newRepos.Add(newRepo);
                }
                return newRepos;
            })
            .SubscribeOn(TaskPoolScheduler.Default)
        )
        .Merge(maxConcurrent: 5);

    var allRepos = new ConcurrentBag<Repo>();

    await observable.ForEachAsync(repos =>
    {
        foreach (var r in repos)
            allRepos.Add(r);
    });

    return Results.Json(allRepos);
});

app.Run();
