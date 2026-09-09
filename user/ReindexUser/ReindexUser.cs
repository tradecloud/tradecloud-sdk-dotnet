using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexUser
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user/private/specs.yaml#/user/reindexForEntityIds
        const string reindexUserUrl = "https://api.accp.tradecloud1.com/v2/user/reindex";

        // Check/amend mandatory user id
        const string jsonContentWithSingleQuotes =
            @"{
              `entityIds`: [``]
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex user example.");

            HttpClient httpClient = new HttpClient();
            if (!EnvFile.TryAccessToken(out var accessToken))
                return;
            await ReindexUser(accessToken);

            async Task ReindexUser(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(reindexUserUrl, content);
                watch.Stop();
                Console.WriteLine("ReindexUser StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("ReindexUser Body: " + responseString);
            }
        }
    }
}
