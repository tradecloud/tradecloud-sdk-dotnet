using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SetDeliveryScheduleStatusBatch
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string fromDate = "2023-01-01";
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderLineSearchUrl = "https://api.accp.tradecloud1.com/v2/order-line-search/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/updateOrderLineDeliverySchedule
        const string setDeliveryScheduleUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{id}/line/{position}/deliverySchedule";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': ['{companyId}']
                },
                'status': {
                    'processStatus': ['Issued', 'InProgress', 'Confirmed', 'Rejected'],
                    'logisticsStatus': ['Delivered']
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
            Console.WriteLine("Tradecloud set delivery schedule status as supplier batch.");
             var jsonDeliveryScheduleTemplate = File.ReadAllText(@"delivery-schedule-template.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            DateTime fromDateTime = DateTime.Parse(fromDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

            using (var log = new StreamWriter("set_delivery_schedule.log", append: true))
            {
                int offset = 0;
                int total = limit;
                while (total > offset && offset < maxTotal)                
                {
                    var queryResult = await SearchOrderLines(offset, log);
                    if (queryResult != null)
                    {
                        total = ((int)queryResult["total"]);
                        //await log.WriteLineAsync("total=" + total + " offset=" + offset);
                        offset += limit;

                        foreach (var orderLine in queryResult.First.Values())
                        {
                            string purchaseOrderNumber = orderLine["buyerOrder"]["purchaseOrderNumber"].ToString();
                            string orderId = buyerId + "-" + purchaseOrderNumber;
                            string orderLinePosition = orderLine["buyerLine"]["position"].ToString();
                            string? row = orderLine["buyerLine"]["row"] != null ? orderLine["buyerLine"]["row"].ToString() : null;
                            string processStatus = orderLine["status"]["processStatus"].ToString();
                            string logisticsStatus = orderLine["status"]["logisticsStatus"].ToString();
                            string deliveryOverdue = orderLine["indicators"]["deliveryOverdue"].ToString();
                            string firstDeliveryDateString =  orderLine["firstDeliveryDate"].ToString();
                            DateTime firstDeliveryDate = DateTime.Parse(firstDeliveryDateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

                            if (firstDeliveryDate >= fromDateTime && logisticsStatus == "Delivered")
                            {                                
                                if (dryRun) 
                                {
                                    await log.WriteLineAsync("SetDeliverySchedule to Open status: purchaseOrderNumber=" + purchaseOrderNumber + " row=" + row + " position=" + orderLinePosition + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus + " deliveryOverdue=" + deliveryOverdue + " firstDeliveryDate=" + firstDeliveryDateString);
                                }
                                else
                                {
                                    await SetDeliveryScheduleStatus(orderId, orderLinePosition, log);
                                }
                            }
                        }
                    }
                    else {
                        total = 0;
                    }
                }

                async Task<JObject> SearchOrderLines(int offset, StreamWriter log)
                {
                    var queryTemplate = queryTemplateWithSingleQuotes.Replace("'", "\"");
                    var query = queryTemplate.Replace("{companyId}", buyerId).Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
                    var content = new StringContent(query, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(orderLineSearchUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    //await log.WriteLineAsync("SearchOrderLines start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    //await log.WriteLineAsync("SearchOrderLines request body=" + query);
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

                async Task SetDeliveryScheduleStatus(string orderId, string orderLinePosition, StreamWriter log)
                {                
                    var setDeliveryScheduleUrl = setDeliveryScheduleUrlTemplate
                        .Replace("{id}", orderId)                    
                        .Replace("{position}", orderLinePosition);
                    
                    // Assumes delivery schedule position is the same as order line position
                    var jsonDeliverySchedule = jsonDeliveryScheduleTemplate.Replace("{position}", orderLinePosition);
                    var content = new StringContent(jsonDeliverySchedule, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(setDeliveryScheduleUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("SetDeliverySchedule orderId=" + orderId + " orderLinePosition=" + orderLinePosition + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    if (statusCode == 400)
                        await log.WriteLineAsync("SetDeliverySchedule request body=" + jsonDeliverySchedule); 
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                        await log.WriteLineAsync("SetDeliverySchedule response body=" +  responseString);
                }
            }
        }
    }
}