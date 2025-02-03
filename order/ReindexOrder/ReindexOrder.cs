using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexOrder
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/reindexForEntityIds
        const string reindexOrderUrl = "https://api.accp.tradecloud1.com/v2/order/reindex";

        // Check/amend mandatory user id
        const string jsonContentWithSingleQuotes =
            @"{
              `entityIds`: [``]
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex order example.");

            HttpClient httpClient = new HttpClient();
            var accessToken = "";
            await ReindexUser(accessToken);

            async Task ReindexUser(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(reindexOrderUrl, content);
                watch.Stop();
                Console.WriteLine("ReindexOrder StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("ReindexOrder Body: " + responseString);
            }
        }
    }
}
