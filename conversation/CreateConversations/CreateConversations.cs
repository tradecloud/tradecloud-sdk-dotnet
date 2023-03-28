using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // INFO: this script will create conversations for one order header and its lines. 
    // If an order header or line already has a conversation, it will be ignored.
    class CreateConversations
    {
        const bool dryRun = true;
        const string orderId = "";
        const string accessToken = "";

       // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/specs.yaml#/order/getOrderByIdRoute
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order/";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/conversation/private/specs.yaml#/conversation/createOrderConversation
        const string createConversationUrl = "https://api.accp.tradecloud1.com/v2/conversation/order/create";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud create conversations.");

            var jsonConversationTemplate = File.ReadAllText(@"conversation-template.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("create_conversations.log", append: true))
            {
                var queryResult = await FindOrderById(log);
                if (queryResult != null)
                {
                    string orderId = queryResult["id"].ToString();
                    string buyerId = queryResult["buyerOrder"]["companyId"].ToString();
                    string supplierId = queryResult["supplierOrder"]["companyId"].ToString();
                    await log.WriteLineAsync("CreateConversations found orderId=" + orderId + " buyerId=" + buyerId + " supplierId=" + supplierId);

                    if (dryRun)
                    {
                        await log.WriteLineAsync("CreateConversations dry run: CreateConversation orderId=" + orderId);
                    }
                    else
                    {
                        await CreateConversation(orderId, null, buyerId, supplierId, log);
                    }
                    foreach (var line in queryResult.SelectToken("lines"))
                    {
                        string lineId = line["id"].ToString();
                        
                        if (dryRun)
                    {
                            await log.WriteLineAsync("CreateConversations dry run: CreateConversation orderId=" + orderId + " lineId=" + lineId);
                        }
                        else
                        {
                            await CreateConversation(orderId, lineId, buyerId, supplierId, log);
                        }
                    }
                }

                async Task<JObject> FindOrderById(StreamWriter log)
                {                
                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(orderSearchUrl + orderId);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("FindOrderById start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200)
                    {
                        //await log.WriteLineAsync(("FindOrderById response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                        return JObject.Parse(responseString);
                    }
                    else
                    {
                        await log.WriteLineAsync("FindOrderById response body=" +  responseString);
                        return null;
                    }
                }

                async Task CreateConversation(string orderId, string lineId, string buyerId, string supplierId, StreamWriter log)
                {                
                    var jsonConversation = "";
                    if (lineId == null)
                    {
                       jsonConversation = jsonConversationTemplate.Replace("\"{orderLineId}\"", "null");
                    }
                    else
                    {
                       jsonConversation = jsonConversationTemplate.Replace("{orderLineId}", lineId);                    
                    }
                    jsonConversation = jsonConversation.Replace("{orderId}", orderId).Replace("{buyerId}", buyerId).Replace("{supplierId}", supplierId);

                    var content = new StringContent(jsonConversation, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(createConversationUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("CreateConversation orderId=" + orderId + " lineId=" + lineId + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    if (statusCode == 400)
                    {
                        await log.WriteLineAsync("CreateConversation request body=" + jsonConversation); 
                    }
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                    {
                        await log.WriteLineAsync("CreateConversation response body=" +  responseString);
                    }
                }
            }
        }
    }
}
