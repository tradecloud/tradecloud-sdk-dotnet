using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CompleteOrderLines
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-line-search/private/specs.yaml#/order-line-search/getByIdRoute
        const string getOrderLineUrlTemplate = "https://api.accp.tradecloud1.com/v2/order-line-search/{id}";    

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/indicators";


        const string indicatorsTemplateWithSingleQuotes = @"{
            'order': {
                'companyId': '{companyId}',
                'purchaseOrderNumber': '{purchaseOrderNumber}'
            },
            'lines': [
                {
                'position': '{position}',
                'indicators': {
                    'completed': true
                }
            }
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud complete order lines.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("complete_order_lines.log", append: true))
            {
                string[] orderNumberAndPositions = System.IO.File.ReadAllLines(@"order_numbers_and_positions.txt");

                foreach (var orderNumberAndPosition in orderNumberAndPositions)
                {
                    await CompleteOrderLine(orderNumberAndPosition);
                }   
                    
                async Task CompleteOrderLine(string orderNumberAndPosition)            
                {  
                    var orderLineId = buyerId + "-" + orderNumberAndPosition;
                    var queryResult = await GetOrderLine(orderLineId, log);
                    if (queryResult != null)
                    {              
                        var processStatus = queryResult["status"]["processStatus"].ToString();
                        var logisticsStatus = queryResult["status"]["logisticsStatus"].ToString();
                        var purchaseOrderNumber = queryResult["buyerOrder"]["purchaseOrderNumber"].ToString();
                        var position = queryResult["buyerLine"]["position"].ToString();

                        if (processStatus != "Completed" && processStatus != "Cancelled")
                        {
                            if (dryRun)
                            {
                                await log.WriteLineAsync("CompleteOrderLines dry run: purchaseOrderNumber=" + purchaseOrderNumber + " position=" + position + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
                            }
                            else
                            {
                                await SendOrderLineIndicators(purchaseOrderNumber, position, log);
                            }
                        }
                        else
                        {
                            await log.WriteLineAsync("CompleteOrderLines: Line is already Completed or Cancelled: purchaseOrderNumber=" + purchaseOrderNumber + " position=" + position + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
                        }
                    }
                    else
                    {
                        await log.WriteLineAsync("CompleteOrderLines: Could not find order line having orderLineId=" + orderLineId);
                    }
                    
                    async Task<JObject> GetOrderLine(string orderLineId, StreamWriter log)
                    {
                        var getOrderLineUrl = getOrderLineUrlTemplate.Replace("{id}", orderLineId);

                        var start = DateTime.Now;
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        var response = await httpClient.GetAsync(getOrderLineUrl);
                        watch.Stop();

                        var statusCode = (int)response.StatusCode;
                        //await log.WriteLineAsync("GetOrderLine url=" + getOrderLineUrl + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                        string responseString = await response.Content.ReadAsStringAsync();
                        if (statusCode == 200)
                        {
                            return JObject.Parse(responseString);
                        }
                        else
                        {
                            await log.WriteLineAsync("GetOrderLine response statusCode=" + statusCode + " body=" + responseString);
                            return null;
                        }
                    }

                    async Task SendOrderLineIndicators(string purchaseOrderNumber, string position, StreamWriter log)
                    {                
                        var indicatorsTemplate = indicatorsTemplateWithSingleQuotes.Replace("'", "\"");
                        var indicators = indicatorsTemplate.Replace("{companyId}", buyerId).Replace("{purchaseOrderNumber}", purchaseOrderNumber).Replace("{position}", position);
                        var content = new StringContent(indicators, Encoding.UTF8, "application/json");

                        var start = DateTime.Now;
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);
                        watch.Stop();

                        var statusCode = (int)response.StatusCode;
                        await log.WriteLineAsync("SendOrderLineIndicators url=" + sendOrderIndicatorsUrl + " purchaseOrderNumber=" + purchaseOrderNumber + " position=" + position + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                        string responseString = await response.Content.ReadAsStringAsync();
                        if (statusCode != 200)
                        {
                            await log.WriteLineAsync("SendOrderLineIndicators response body=" +  responseString);
                        }
                    }
                }
            }
        }
    }
}