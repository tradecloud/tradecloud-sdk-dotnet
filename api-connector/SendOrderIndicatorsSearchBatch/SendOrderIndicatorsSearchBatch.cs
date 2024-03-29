﻿using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderIndicatorsSearchBatch
    {
        const bool dryRun = true;
        const string accessToken = "";
        const string buyerId = "";
        const string supplierId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderLineSearchUrl = "https://api.accp.tradecloud1.com/v2/order-line-search/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/indicators";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': ['{buyerId}']
                },
                'supplierOrder': {
                    'companyId': ['{supplierId}']
                },              
                'status': {
                    'processStatus': ['Completed'],
                    'logisticsStatus': ['Open']
                },
                'indicators': {
                    'deliveryOverdue': true
                }
            },
            'sort':[{'field':'buyerOrder.purchaseOrderNumber','order':'asc'}],
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;
        const int maxTotal = 10000;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send indicators batch.");
             var jsonOrderIndicatorsTemplate = File.ReadAllText(@"order-indicators-template.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("send_order_indicators_search_batch.log", append: true))
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
                            string deliveryOverdue = order["indicators"]["deliveryOverdue"].ToString();

                            if (!position.StartsWith("0") && processStatus == "Completed" && logisticsStatus == "Open")
                            {                                
                                if (dryRun) 
                                {
                                    await log.WriteLineAsync("purchaseOrderNumber=" + purchaseOrderNumber + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus + " deliveryOverdue=" + deliveryOverdue);
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
                    var query = queryTemplate
                        .Replace("{buyerId}", buyerId)
                        .Replace("{supplierId}", supplierId)
                        .Replace("{offset}", offset.ToString())
                        .Replace("{limit}", limit.ToString());
                    var content = new StringContent(query, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(orderSearchUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("SearchOrderLines start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    await log.WriteLineAsync("SearchOrderLines request body=" + query);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200)
                    {
                        return JObject.Parse(responseString);
                    }
                    else
                    {
                        await log.WriteLineAsync("SearchOrderLines response body=" + responseString);
                        return null;
                    }
                }

                async Task SendOrderIndicators(string purchaseOrderNumber, StreamWriter log)
                {                
                    var jsonOrderIndicators = jsonOrderIndicatorsTemplate
                        .Replace("{companyId}", buyerId)                    
                        .Replace("{purchaseOrderNumber}", purchaseOrderNumber);
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
}