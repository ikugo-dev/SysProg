using System.Text.RegularExpressions;
using System.Reactive.Linq;

public class Helper
{
    public static async Task<List<string>> GetLinks(HttpClient client, string topic, int pageLimit, int perPage)
    {
        var lastPage = 1;
        var links = new List<string>();

        var url = $"https://api.github.com/search/repositories?q=topic:{Uri.EscapeDataString(topic)}+fork:true&per_page={perPage}";

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

        if (lastPage == 1) links.Add(url);
        else
        {
            for (int i = 1; i <= Math.Min(pageLimit, lastPage); i++)
            {
                string newurl = $"https://api.github.com/search/repositories?q="
                    + $"topic:{Uri.EscapeDataString(topic)}"
                    + $"+fork:true"
                    + $"&per_page={perPage}"
                    + $"&page={i}";
                links.Add(newurl);
            }
        }
        return links;
    }
}
