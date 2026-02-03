using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexAllOrders
    {
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/reindexAll
        const string reindexAllOrdersUrl = "https://api.accp.tradecloud1.com/v2/order/reindex/all";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex all orders example.");

            HttpClient httpClient = new HttpClient();
            await ReindexAllOrders(accessToken);

            async Task ReindexAllOrders(string accessToken)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(reindexAllOrdersUrl, null);
                    watch.Stop();
                    Console.WriteLine("ReindexAllOrders StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("ReindexAllOrders Body: " + responseString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
