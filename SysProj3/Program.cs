using Newtonsoft.Json;
using SysProj3;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Linq;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseFileServer(); // iskomentarisite liniju ako zelite cist api bez index.html

app.MapGet("/api", async (string topic, int pageLimit, int perPage) =>
{
    if (string.IsNullOrWhiteSpace(topic))
        return Results.BadRequest(new { error = "Missing topic parameter" });

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.mercy-preview+json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SysProj3");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");
    //PAT token used only for higher request limit

    List<string> urls = await Helper.GetLinks(client, topic, pageLimit, perPage);
    var cts = new CancellationTokenSource();

    var observable = urls
        .ToObservable()
        .SelectMany((url, index) => Observable.Timer(TimeSpan.FromSeconds(3 * index))
            // ukoliko se dobija 403 error, moze da se napravi da se unosi koliki razmak izmedju taskova u kom slucaju je red suvisan a x=url
            // alternativa bi bila da se u slucaju 403 errora pozove funkcija ispocetka sa 3*index delay, ali me nesto mrzi 
            .SelectMany(x => Observable.FromAsync(async () =>
            {
                cts.Token.ThrowIfCancellationRequested();

                var newRepos = new List<Repo>();

                Console.WriteLine($"Fetching {url} on thread {Thread.CurrentThread.ManagedThreadId}");

                var response = await client.GetAsync(url);
                //Console.WriteLine(response.Headers);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(content);
                dynamic repos = JsonConvert.DeserializeObject<dynamic>(content).items;
                var numberOfRepos = JsonConvert.DeserializeObject<dynamic>(content).total_count;


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
        ));

    var allRepos = new ConcurrentBag<Repo>();

    await observable.ForEachAsync(repos =>
    {
        foreach (var r in repos)
            allRepos.Add(r);
    });

    return Results.Json(allRepos);
});

app.Run();
