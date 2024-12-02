using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

namespace Com.Tradecloud1.SDK.Client
{
    // WARN: this script will cancel, complete or deliver orders, which cannot be reverted. 
    class SendOrderIndicatorsCsvBatch
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/indicators";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order indicators CVS batch.");

            var jsonOrderIndicatorsTemplate = File.ReadAllText(@"order-indicators-template.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { IgnoreBlankLines = true, Encoding = Encoding.UTF8 };

            using (var log = new StreamWriter("send_indicators_csv_batch.log", append: true))
            using (var reader = new StreamReader("orders.csv"))
            using (var csvReader = new CsvReader(reader, config))
            {
                csvReader.Context.RegisterClassMap<OrderMap>();
                var orders = csvReader.GetRecords<Order>();
                foreach (var order in orders)
                {                  
                    await SendOrderIndicators(order.purchaseOrderNumber.Trim(), log);
                }
            }

            async Task SendOrderIndicators(string purchaseOrderNumber, StreamWriter log)
            {                
                var jsonOrderIndicators = jsonOrderIndicatorsTemplate
                    .Replace("{companyId}", buyerId)                    
                    .Replace("{purchaseOrderNumber}", purchaseOrderNumber);

                if (dryRun) 
                {
                    await log.WriteLineAsync("SendOrderIndicators dry run purchaseOrderNumber=" + purchaseOrderNumber + " content=" + jsonOrderIndicators);
                }    
                else
                {
                    var content = new StringContent(jsonOrderIndicators, Encoding.UTF8, "application/json");
                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("SendOrderIndicators purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    if (statusCode == 400)
                        await log.WriteLineAsync("SendOrderIndicators request body=" + jsonOrderIndicators); 
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                        await log.WriteLineAsync("SendOrderIndicators response body=" +  responseString);
                }
            }
        }
    }

    public class Order
    {
        public string purchaseOrderNumber { get; set; }
    }

    public sealed class OrderMap : ClassMap<Order>
    {
        public OrderMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
