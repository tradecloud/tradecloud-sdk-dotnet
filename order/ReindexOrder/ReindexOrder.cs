using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexOrder
    {
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/reindexForEntityIds
        const string reindexOrderUrl = "https://api.accp.tradecloud1.com/v2/order/reindex";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex order example.");

            HttpClient httpClient = new HttpClient();
            await ReindexOrder(accessToken);

            async Task ReindexOrder(string accessToken)
            {
                try
                {
                    // Read order IDs from file
                    string[] orderIds = File.ReadAllLines("orderIds.txt")
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToArray();

                    if (orderIds.Length == 0)
                    {
                        Console.WriteLine("No order IDs found in orderIds.txt file.");
                        return;
                    }

                    Console.WriteLine($"Found {orderIds.Length} order IDs to reindex.");

                    // Build JSON with order IDs
                    var entityIdsJson = string.Join(",", orderIds.Select(id => $"\"{id.Trim()}\""));
                    var jsonContent = $@"{{
                        ""entityIds"": [{entityIdsJson}]
                    }}";

                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(reindexOrderUrl, content);
                    watch.Stop();
                    Console.WriteLine("ReindexOrder StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("ReindexOrder Body: " + responseString);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Error: orderIds.txt file not found. Please create the file with one order ID per line.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
