using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CleanOrderLines
    {   
        const bool dryRun = true;
        const string buyerId = "";
        const string supplierId = "";
        const string accessToken = "";
        const int limit = 100;
        const string esUrlTemplate = "http://localhost:9200/prod-order-line/_doc/{buyerId}-{purchaseOrderNumber}-{linePosition}";
        // Fill in mandatory username

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderLineSearchUrl = "https://api.accp.tradecloud1.com/v2/order-line-search/search";
        
        // Fill in the search query
        const string searchBodyTemplate = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': [
                        '{buyerId}'
                    ]
                },
                'supplierOrder': {
                    'companyId': [
                        '{supplierId}'
                    ]
                }
            },
            'sort': [
                {
                'field': 'buyerOrder.erpIssueDateTime',
                'order': 'asc'
                }
            ],
            'offset': {offset},
            'limit': {limit}
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud clean order lines.");
            
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

            int offset = 0;
            int total = limit;
            while (total > offset)                
            {
                var queryResult = await SearchOrderLines(offset);
                if (queryResult != null)
                {
                    total = ((int)queryResult["total"]);
                    Console.WriteLine("total=" + total + " offset=" + offset);
                    offset += limit;

                    foreach (var line in queryResult.First.Values())
                    {
                        var purchaseOrderNumber = line["buyerOrder"]["purchaseOrderNumber"].ToString();
                        var linePostion = line["buyerLine"]["position"].ToString();
                        if (!linePostion.StartsWith("0"))
                        {
                            await DeleteOrderLine(purchaseOrderNumber, linePostion);
                        }                        
                    }
                }
                else {
                    total = 0;
                }
            }
            
            async Task<JObject> SearchOrderLines(int offset)
            {                
                var searchBody = searchBodyTemplate.Replace("'", "\"").Replace("{buyerId}", buyerId).Replace("{supplierId}", supplierId).Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
                var content = new StringContent(searchBody, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(orderLineSearchUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchOrderLines start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    Console.WriteLine("SearchOrderLines request body=" + searchBody); 
                    Console.WriteLine("SearchOrderLines response body=" + responseString);
                    return null;
                }
            }

            async Task DeleteOrderLine(string purchaseOrderNumber, string linePosition)
            {                
                var url = esUrlTemplate.Replace("{buyerId}", buyerId).Replace("{purchaseOrderNumber}", purchaseOrderNumber).Replace("{linePosition}", linePosition);                

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();        
            
                if (dryRun)
                {
                    var response = await httpClient.GetAsync(url);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    Console.WriteLine("DeleteOrderLine dryrun " + purchaseOrderNumber + "-" + linePosition + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " url=" + url);

                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200) {
                        Console.WriteLine("DeleteOrderLine dryrun response body=" +  responseString);                    
                    }
                } 
                else
                {
                    var response = await httpClient.DeleteAsync(url);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    Console.WriteLine("DeleteOrderLine " + purchaseOrderNumber + "-" + linePosition + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200) {
                        Console.WriteLine("DeleteOrderLine response body=" +  responseString);                    
                    }
                }
            }
        }
    }
}
