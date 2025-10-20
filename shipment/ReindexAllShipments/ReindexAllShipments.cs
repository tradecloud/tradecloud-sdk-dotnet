using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexAllShipments
    {
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/shipment/private/specs.yaml#/shipment/reindexAll
        const string reindexAllShipmentsUrl = "https://api.accp.tradecloud1.com/v2/shipment/reindex/all";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex all shipments example.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await ReindexAllShipments();

            async Task ReindexAllShipments()
            {
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(reindexAllShipmentsUrl, null);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ReindexAllShipments start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("ReindexAllShipments response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("ReindexAllShipments response body=" + responseString);
            }
        }
    }
}
