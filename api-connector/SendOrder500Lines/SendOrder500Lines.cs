using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder500Lines
    {
        // API manual: 500 lines in total per order. One delivery per line stays under the 100 delivery-lines limit.
        const int lineCount = 500;
        const string templateFileName = "minimal-order.json";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order";

        static async Task Main(string[] args)
        {
            EnvFile.Load();
            if (!EnvFile.TryUsernamePassword(out var username, out var password))
                return;

            var order = BuildOrder();
            var purchaseOrderNumber = (string)order["order"]["purchaseOrderNumber"];
            var jsonContent = order.ToString(Formatting.None);
            Console.WriteLine("Tradecloud send order with " + lineCount + " lines, purchaseOrderNumber=" + purchaseOrderNumber + ", bytes=" + Encoding.UTF8.GetByteCount(jsonContent));

            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(sendOrderUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("SendOrder start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            string responseString = await response.Content.ReadAsStringAsync();
            if (statusCode == 200)
                Console.WriteLine("SendOrder response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
            else
                Console.WriteLine("SendOrder response body=" + responseString);
        }

        // Copies the single line in minimal-order.json. Positions stay unique: 00010, 00020, ...
        static JObject BuildOrder()
        {
            var order = JObject.Parse(File.ReadAllText(templateFileName));
            var lineTemplate = (JObject)order["lines"][0];
            var lines = new JArray();
            for (var i = 1; i <= lineCount; i++)
            {
                var line = (JObject)lineTemplate.DeepClone();
                var position = (i * 10).ToString("00000");
                line["position"] = position;
                line["item"]["number"] = "12345-" + position;
                lines.Add(line);
            }

            order["lines"] = lines;
            order["order"]["purchaseOrderNumber"] = "PO" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return order;
        }
    }
}
