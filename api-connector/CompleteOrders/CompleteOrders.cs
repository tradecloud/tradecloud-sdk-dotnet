using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // WARNING: this script will Complete all orders of specified companyId. This cannot be reverted.
    class CompleteOrders
    {
        const bool dryRun = true;
        const string companyId = "";
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
                }
            },
            'sort':[{'field':'buyerOrder.purchaseOrderNumber','order':'asc'}],
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;

        const string indicatorsTemplateWithSingleQuotes = @"{
            'order': {
                'companyId': '{companyId}',
                'purchaseOrderNumber': '{purchaseOrderNumber}',
                'indicators': {
                    'completed': true
                }
            }
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud complete orders.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("complete_orders.log", append: true))
            {
                int offset = 0;
                int total = limit;
                while (total > offset)                
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

                            if (processStatus != "Completed" && processStatus != "Cancelled")
                            {
                                if (dryRun)
                                {
                                    await log.WriteLineAsync("CompleteOrders dry run: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
                                }
                                else
                                {
                                    await SendOrderIndicators(purchaseOrderNumber, log);
                                }
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
                    var query = queryTemplate.Replace("{companyId}", companyId).Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
                    var content = new StringContent(query, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(orderSearchUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    //await log.WriteLineAsync("SearchOrders start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    //await log.WriteLineAsync("SearchOrders request body=" + query);
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
                    var indicators = indicatorsTemplate.Replace("{companyId}", companyId).Replace("{purchaseOrderNumber}", purchaseOrderNumber);
                    var content = new StringContent(indicators, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("CompleteOrders purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                    {
                        await log.WriteLineAsync("CompleteOrders response body=" +  responseString);
                    }
                }
            }
        }
    }
}