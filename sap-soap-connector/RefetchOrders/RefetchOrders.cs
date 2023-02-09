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
    class RefetchOrders
    {
        const string accessToken = "";
        const string companyId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string fetchPurchaseOrderUrlTemplate = "https://api.accp.tradecloud1.com/v2/sap-soap-connector/company/{companyId}/order/{purchaseOrderNumber}/fetch";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': ['{companyId}']
                },
                'status': {
                    'processStatus': ['Issued'],
                    'logisticsStatus': ['Open']
                }
            },
            'sort':[{'field':'buyerOrder.purchaseOrderNumber','order':'asc'}],
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud refetch orders.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("refetch_orders.log", append: true))
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

                            await RefetchOrders(purchaseOrderNumber, log);
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

                async Task RefetchOrders(string purchaseOrderNumber, StreamWriter log)
                {                
                    var fetchPurchaseOrderUrl = fetchPurchaseOrderUrlTemplate.Replace("{companyId}", companyId).Replace("{purchaseOrderNumber}", purchaseOrderNumber);

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(fetchPurchaseOrderUrl);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("FetchOrder purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                    {
                        await log.WriteLineAsync("FetchOrder response body=" +  responseString);
                    }
                }
            }
        }
    }
}