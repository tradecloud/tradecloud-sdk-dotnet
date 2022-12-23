using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CleanUpInactiveOrders
    {
        const string accessToken = "";
        const string companyId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/indicators";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': [
                        '{companyId}'
                    ]
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
                    'delivered': true,
                    'completed': true
                }
            }
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud clean up inactive orders.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var activePurchasheOrderNumbers = new List<string>(File.ReadAllLines("ActivePurchaseOrderNumbers.txt"));

            using (var log = new StreamWriter("CleanUpInactiveOrders.log", append: true))
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

                            if (activePurchasheOrderNumbers.Contains(purchaseOrderNumber))
                            {
                                // Order must be active: not be Completed and not be Delivered
                                if (processStatus == "Completed" && logisticsStatus == "Delivered")
                                {
                                    await log.WriteLineAsync("Warning, order must be active: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
                                }
                                else
                                {
                                    await log.WriteLineAsync("Skipping, order is active as expected: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);                                    
                                }
                            }
                            else
                            {
                                // Order must not be active: Completed and Delivered
                                if (processStatus == "Completed" && logisticsStatus == "Delivered")
                                {
                                    await log.WriteLineAsync("Skipping, order is NOT active as expected: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
                                    
                                }
                                else
                                {
                                    await log.WriteLineAsync("Completing, order must NOT be active: purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
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
                    await log.WriteLineAsync("SendOrderIndicators purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                    {
                        await log.WriteLineAsync("SendOrderIndicators response body=" +  responseString);
                    }
                }
            }
        }
    }
}