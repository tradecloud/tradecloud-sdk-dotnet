using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class PublishActiveOrder
    {

        // Fill in mandatory orderId
        const string orderId = "902a8f50-b7da-11e5-a837-0800200c9a66-90065931";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/publishActiveOrder
        const string publishActiveOrderUrlTemplate = "https://api.tradecloud1.com/v2/order/{orderId}/publishActiveOrder";

        static async Task Main(string[] args)
        {
            EnvFile.Load();
            if (!EnvFile.TryAccessToken(out var token))
                return;

            Console.WriteLine("Tradecloud publish active order example.");

            var jsonContentTemplate = File.ReadAllText(@"publish-active-order.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await PublishActiveOrder();

            async Task PublishActiveOrder()
            {
                var publishActiveOrderUrl = publishActiveOrderUrlTemplate.Replace("{orderId}", orderId);
                var jsonContent = jsonContentTemplate.Replace("{newDate}", DateTime.Now.ToString("yyyy-MM-dd"));

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(publishActiveOrderUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("PublishActiveOrder start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("PublishActiveOrder request body=" + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("PublishActiveOrder response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("PublishActiveOrder response body=" + responseString);
            }
        }
    }
}
