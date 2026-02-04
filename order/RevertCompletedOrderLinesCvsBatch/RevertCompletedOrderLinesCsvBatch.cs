using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class RevertCancelledOrderLinesCsvBatch
    {
        const string accessToken = "";
        const string companyId = "";
        const string body = "revert.json";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/revertCancelledOrderLines
        const string revertUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/revertCompleted";
        const string completeUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/complete";
        const string deliverUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/deliver";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud revert completed order lines CSV batch.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ",", Encoding = Encoding.UTF8 };

            var jsonContentTemplate = File.ReadAllText(@body);
            using (var log = new StreamWriter("revert.log", append: true))
            using (var reader = new StreamReader("2026-02-04 Tradecloud-One-order-lines-export.csv"))
            using (var csvReader = new CsvReader(reader, csvConfig))
            {
                csvReader.Context.RegisterClassMap<RevertOrderLineMap>();
                var revertOrderLines = csvReader.GetRecords<RevertOrderLine>();
                foreach (var revertOrderLine in revertOrderLines)
                {
                    await RevertCompletedOrderLine(revertOrderLine, log);
                    //await CompleteOrderLine(revertOrderLine, log);
                    //await DeliverOrderLine(revertOrderLine, log);
                }
            }

            async Task RevertCompletedOrderLine(RevertOrderLine revertOrderLine, StreamWriter log)
            {
                var url = HttpUtility.UrlPathEncode(revertUrlTemplate.Replace("{orderId}", companyId + "-" + revertOrderLine.purchaseOrderNumber));
                var jsonContent = jsonContentTemplate.Replace("{position}", revertOrderLine.position);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(url, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("RevertCompletedOrderLine url=" + url + " body=" + jsonContent + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    await log.WriteLineAsync("RevertCompletedOrderLine request body = " + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    await log.WriteLineAsync("RevertCompletedOrderLine response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    await log.WriteLineAsync("RevertCompletedOrderLine response body=" + responseString);
            }

            async Task CompleteOrderLine(RevertOrderLine revertOrderLine, StreamWriter log)
            {
                var url = HttpUtility.UrlPathEncode(completeUrlTemplate.Replace("{orderId}", companyId + "-" + revertOrderLine.purchaseOrderNumber));
                var jsonContent = jsonContentTemplate.Replace("{position}", revertOrderLine.position);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("CompleteOrderLine url=" + url + " body=" + jsonContent + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    await log.WriteLineAsync("CompleteOrderLine request body = " + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    await log.WriteLineAsync("CompleteOrderLine response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    await log.WriteLineAsync("CompleteOrderLine response body=" + responseString);
            }

            async Task DeliverOrderLine(RevertOrderLine revertOrderLine, StreamWriter log)
            {
                var url = HttpUtility.UrlPathEncode(deliverUrlTemplate.Replace("{orderId}", companyId + "-" + revertOrderLine.purchaseOrderNumber));
                var jsonContent = jsonContentTemplate.Replace("{position}", revertOrderLine.position);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("DeliverOrderLine url=" + url + " body=" + jsonContent + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    await log.WriteLineAsync("DeliverOrderLine request body = " + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    await log.WriteLineAsync("DeliverOrderLine response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    await log.WriteLineAsync("DeliverOrderLine response body=" + responseString);
            }
        }
    }

    public class RevertOrderLine
    {
        public string purchaseOrderNumber { get; set; }
        public string position { get; set; }
    }

    public sealed class RevertOrderLineMap : ClassMap<RevertOrderLine>
    {
        public RevertOrderLineMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
