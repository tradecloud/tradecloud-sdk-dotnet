using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class RefetchOrders
    {
        const string accessToken = "";
        const string companyId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string fetchPurchaseOrderUrlTemplate = "https://api.accp.tradecloud1.com/v2/sap-soap-connector/company/{companyId}/order/{purchaseOrderNumber}/fetch";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud fetch orders from file batch.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("fetch_orders.log", append: true))
            {
                using (var reader = new StreamReader("purchase-order-numbers.txt"))
                {
                    while (!reader.EndOfStream)
                    {
                        var purchaseOrderNumber = reader.ReadLine();
                        await FetchOrder(purchaseOrderNumber, log);
                    }
                }
            }

            async Task FetchOrder(string purchaseOrderNumber, StreamWriter log)
            {
                var fetchPurchaseOrderUrl = fetchPurchaseOrderUrlTemplate.Replace("{companyId}", companyId).Replace("{purchaseOrderNumber}", purchaseOrderNumber);

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.GetAsync(fetchPurchaseOrderUrl);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("FetchOrder purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200)
                {
                    await log.WriteLineAsync("FetchOrder response body=" + responseString);
                }
            }
        }
    }
}