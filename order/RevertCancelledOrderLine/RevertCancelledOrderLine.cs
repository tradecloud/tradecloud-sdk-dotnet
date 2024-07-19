using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class RevertCancelledOrderLine
    {
        const string accessToken = "";
        const string orderId = "";
        const string body = "revert.json";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/revertCancelledOrderLines
        const string revertUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/revertCancelled";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud revert cancelled order line example.");

            var jsonContent = File.ReadAllText(@body);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await RevertCancelledOrderLine();

            async Task RevertCancelledOrderLine()
            {
                var revertUrl = HttpUtility.UrlPathEncode(revertUrlTemplate.Replace("{orderId}", orderId));
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(revertUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("RevertCancelledOrderLine start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("RevertCancelledOrderLine request body=" + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("RevertCancelledOrderLine response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("RevertCancelledOrderLine response body=" + responseString);
            }
        }
    }
}
