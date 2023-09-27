using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // WARNING: this script will Deliver all migrated Completed orders of specified companyId. This cannot be reverted.
    class DeliverMigratedOrders
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/indicators";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': ['{companyId}']
                },
                'status': {
                    'processStatus': ['Completed'],
                    'logisticsStatus': ['Open']
                }
            },
            'sort':[{'field':'buyerOrder.purchaseOrderNumber','order':'asc'}],
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;
        const int maxTotal = 10000;

        const string indicatorsTemplateWithSingleQuotes = @"{
            'order': {
                'companyId': '{companyId}',
                'purchaseOrderNumber': '{purchaseOrderNumber}',
                'indicators': {
                    'delivered': true
                }
            }
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud deliver Completed migrated orders.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("deliver_orders.log", append: true))
            {
                int offset = 0;
                int total = limit;
                while (total > offset && offset < maxTotal)
                {
                    var queryResult = await SearchOrders(offset, log);
                    if (queryResult != null)
                    {
                        total = ((int)queryResult["total"]);
                        await log.WriteLineAsync("total=" + total + " offset=" + offset);
                        offset += limit;

                        foreach (var order in queryResult.First.Values())
                        {
                            string purchaseOrderNumber = order["buyerOrder"]["purchaseOrderNumber"].ToString();
                            string processStatus = order["status"]["processStatus"].ToString();
                            string logisticsStatus = order["status"]["logisticsStatus"].ToString();
                            string origin = null;
                            if (order["meta"]["source"]["origin"] != null)
                            {
                                origin = order["meta"]["source"]["origin"].ToString();
                            }
                            string firstDeliveryDateString = order["firstDeliveryDate"].ToString();
                            DateTime firstDeliveryDate = DateTime.Parse(firstDeliveryDateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

                            if (processStatus == "Completed" && logisticsStatus != "Delivered")
                            //if ((origin == "Legacy" || origin == null || firstDeliveryDate <  new DateTime(2023, 1, 1)) && processStatus == "Completed" && logisticsStatus != "Delivered")
                            {
                                if (dryRun)
                                {
                                    await log.WriteLineAsync("DeliverMigratedOrders dry run: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus + " origin=" + origin + " firstDeliveryDate=" + firstDeliveryDate);
                                }
                                else
                                {
                                    await SendOrderIndicators(purchaseOrderNumber, log);
                                }
                            }
                            else
                            {
                                await log.WriteLineAsync("DeliverMigratedOrders skip: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus + " origin=" + origin + " firstDeliveryDate=" + firstDeliveryDate);
                            }
                        }
                    }
                    else {
                        total = 0;
                    }
                }

                async Task<JObject> SearchOrders(int offset, StreamWriter log)
                {
                    var queryTemplate = queryTemplateWithSingleQuotes.Replace("'", "\"");
                    var query = queryTemplate.Replace("{companyId}", buyerId).Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
                    var content = new StringContent(query, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(orderSearchUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("SearchOrders start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    await log.WriteLineAsync("SearchOrders request body=" + query);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200)
                    {
                        return JObject.Parse(responseString);
                    }
                    else
                    {
                        await log.WriteLineAsync("SearchOrders response body=" + responseString);
                        return null;
                    }
                }

                async Task SendOrderIndicators(string purchaseOrderNumber, StreamWriter log)
                {                
                    var indicatorsTemplate = indicatorsTemplateWithSingleQuotes.Replace("'", "\"");
                    var indicators = indicatorsTemplate.Replace("{companyId}", buyerId).Replace("{purchaseOrderNumber}", purchaseOrderNumber);
                    var content = new StringContent(indicators, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("DeliverMigratedOrders purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                    {
                        await log.WriteLineAsync("DeliverMigratedOrders response body=" +  responseString);
                    }
                }
            }
        }
    }
}