using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexAllTasks
    {
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/reindexAll
        const string reindexAllTasksUrl = "https://api.accp.tradecloud1.com/v2/workflow/reindex/all";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex all tasks example.");

            HttpClient httpClient = new HttpClient();
            await ReindexAllTasks(accessToken);

            async Task ReindexAllTasks(string accessToken)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(reindexAllTasksUrl, null);
                    watch.Stop();
                    Console.WriteLine("ReindexAllTasks StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("ReindexAllTasks Body: " + responseString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
