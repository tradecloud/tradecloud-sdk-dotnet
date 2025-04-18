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
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.test.tradecloud1.com/v2/api-connector/order/indicators";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order indicators CVS batch.");

            var orderIndicatorsJsonTemplate = File.ReadAllText(@"order-line-indicators-cancelled.json");

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
                    await SendOrderIndicators(order.companyId, order.purchaseOrderNumber, log);
                }
            }

            async Task SendOrderIndicators(string companyId, string purchaseOrderNumber, StreamWriter log)
            {                
                var jsonContent = orderIndicatorsJsonTemplate
                    .Replace("{companyId}", companyId)                    
                    .Replace("{purchaseOrderNumber}", purchaseOrderNumber);

                if (dryRun) 
                {
                    await log.WriteLineAsync($"SendOrderIndicators dry run companyId={companyId} purchaseOrderNumber={purchaseOrderNumber} content={jsonContent}");
                }    
                else
                {
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    if (statusCode == 400)
                        await log.WriteLineAsync("SendOrderIndicators request body=" + jsonContent);                     
                    if (statusCode != 200)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        await log.WriteLineAsync("SendOrderIndicators response body=" +  responseString);
                    }
                }
            }
        }
    }

    public class Order
    {
        public string companyId { get; set; }
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
