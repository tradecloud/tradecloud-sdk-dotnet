using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class FindOrdersByNumber
    {
        const string accessToken = "";

        // Prepend companyId and dash to each purchase order number from the file to form the Tradecloud order id.
        const string companyId = "";
        const string dash = "-";

        const string purchaseOrderNumbersFile = @"purchaseOrderNumbers.txt";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/specs.yaml#/order/getOrderByIdRoute
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order/";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud find orders by purchase order number.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            string[] lines = File.ReadAllLines(purchaseOrderNumbersFile);
            int found = 0;
            int missing = 0;

            foreach (var line in lines)
            {
                var purchaseOrderNumber = line.Trim();
                if (purchaseOrderNumber.Length == 0)
                    continue;

                var orderId = companyId + dash + purchaseOrderNumber;

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.GetAsync(orderSearchUrl + orderId);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                string responseString = await response.Content.ReadAsStringAsync();

                if (statusCode == 200)
                {
                    found++;
                    Console.WriteLine("FindOrder purchaseOrderNumber=" + purchaseOrderNumber + " orderId=" + orderId + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " EXISTS");
                    Console.WriteLine("FindOrder response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                }
                else
                {
                    missing++;
                    Console.WriteLine("FindOrder purchaseOrderNumber=" + purchaseOrderNumber + " orderId=" + orderId + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " NOT FOUND");
                    Console.WriteLine("FindOrder response body=" + responseString);
                }
            }

            Console.WriteLine("Summary: exists=" + found + " notFoundOrError=" + missing);
        }
    }
}
