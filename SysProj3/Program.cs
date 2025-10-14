using Newtonsoft.Json;
using SysProj3;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

public class Helper
{
    public string? GetTopic()               //temporary function dok se ne napravi interfejs
    {
        Console.WriteLine("Enter desired topic! :3");
        return Console.ReadLine();
    }
    public async Task<List<string>> GetLinks(HttpClient client)
    {

        var topic = GetTopic();
        var lastPage = 1;
        var links = new List<string>();

        var url = $"https://api.github.com/search/repositories?q=topic:{Uri.EscapeDataString(topic)}+fork:true&per_page=100";

        var response = await client.GetAsync(url);


        if (response.Headers.TryGetValues("Link", out IEnumerable<string> headerValues))
        {
            Regex regex = new Regex(@"\d+"); // Matches the last digits in the Link header
            MatchCollection matches = regex.Matches(headerValues.FirstOrDefault());
            //Console.WriteLine(headerValues.FirstOrDefault());


            if (int.TryParse(matches.Last().Value, out lastPage))
            {
                Console.WriteLine($"There are {lastPage} pages of data.");
            }
            else
            {
                Console.WriteLine("Something went wrong.");
            }

        }

        string[] dateConstraints = [

            "+created:2008-01-01..2008-12-31",
            "+created:2009-01-01..2009-12-31",
            "+created:2010-01-01..2010-12-31",
            "+created:2011-01-01..2011-12-31",
            "+created:2012-01-01..2012-12-31",
            "+created:2013-01-01..2013-12-31",
            "+created:2014-01-01..2014-12-31",
            "+created:2015-01-01..2015-12-31",
            "+created:2016-01-01..2016-12-31",
            "+created:2017-01-01..2017-12-31",
            "+created:2018-01-01..2018-12-31",
            "+created:2019-01-01..2019-12-31",
            "+created:2020-01-01..2020-12-31",
            "+created:2021-01-01..2021-12-31",
            "+created:2022-01-01..2022-12-31",
            "+created:2023-01-01..2023-12-31",
            "+created:2024-01-01..2024-12-31",
            "+created:2025-01-01..2025-12-31"
        ];

        if (lastPage == 1) links.Add(url);
        else
        {
            for (int i = 1; i <= lastPage; i++)
            {

                string tempUrl = $"https://api.github.com/search/repositories?q=topic:{Uri.EscapeDataString(topic)}";
                string newUrl;
                foreach( var constraint in dateConstraints)
                {
                    newUrl = tempUrl + constraint + "+fork:true&per_page=100" + $"&page={i}"; 
                    links.Add(newUrl);
                }
            }
        }

        return links;

    }
}


public class Program
{
    public static void Main()

    {

        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.mercy-preview+json");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SysProj3");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");
        //PAT token used only for higher request limit


        Helper help = new Helper();

        List<string> urls = help.GetLinks(client).Result;
        var cts = new CancellationTokenSource();

        var observable = urls
            .ToObservable()
            .SelectMany((url, index) => Observable.Timer(TimeSpan.FromSeconds(3*index)) // ukoliko se dobija 403 error, moze da se napravi da se unosi koliki razmak izmedju taskova u kom slucaju je red suvisan a x=url
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

        observable.Subscribe(

            repos => { // OnNext
                foreach (var repo in repos)
                {
                    Console.WriteLine($"Name: {repo.Name}: ");
                    Console.WriteLine($"    Stars: {repo.Stars},");
                    Console.WriteLine($"    Size: {repo.Size},");
                    Console.WriteLine($"    Forks: {repo.Forks}.");
                    allRepos.Add(repo);
                }
            },
            ex => //OnError  
            {
                Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: Error: {ex.Message}");
                cts.Cancel(); //stops other tasks if there's been an exception
            },
            () => Console.WriteLine($"All repos retrieved. Total number of repos is {allRepos.Count}") //OnCompleted
        );

        
        Console.ReadLine();
        
    }

}