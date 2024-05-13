using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // please note: this script will acknowledge max. 1000 orders.
    class AcknowledgeOrdersBatch
    {
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/listPurchaseOrders
        const string getUnacknowledgedOrdersUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrder?acknowledged=false&limit=1";

        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/acknowledgePurchaseOrder
        const string acknowledgePurchaseOrderUrlTemplate = "https://accp.tradecloud.nl/api/v1/purchaseOrder/{purchaseOrderId}/_acknowledge";

        const string jsonTemplate = @"{
            ""versionHash"": ""{versionHash}""
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud acknowledge order example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);

            using (var log = new StreamWriter("acknowledge_orders_batch.log", append: true))
            {
                var response = await GetUnacknowledgedOrders(log);
                if (response != null)
                {
                    var total = response["total"].ToString();
                    await log.WriteLineAsync("AcknowledgeOrdersBatch total=" + total);

                    var unackedOrders = (JArray)response["data"];
                    foreach (var unackedOrder in unackedOrders)
                    {
                        string purchaseOrderId = unackedOrder["id"].ToString();
                        string versionHash = unackedOrder["versionHash"].ToString();
                        await AcknowledgeOrder(purchaseOrderId, versionHash, log);
                    }
                }

                async Task<JObject> GetUnacknowledgedOrders(StreamWriter log)
                {
                    var response = await httpClient.GetAsync(getUnacknowledgedOrdersUrl);
                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("GetUnacknowledgedOrders status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200)
                    {
                        return JObject.Parse(responseString);
                    }
                    else
                    {
                        await log.WriteLineAsync("GetUnacknowledgedOrders response body=" + responseString);
                        return null;
                    }
                }

                async Task AcknowledgeOrder(string purchaseOrderId, string versionHash, StreamWriter log)
                {
                    var acknowledgePurchaseOrderUrl = acknowledgePurchaseOrderUrlTemplate.Replace("{purchaseOrderId}", purchaseOrderId);
                    var json = jsonTemplate.Replace("'", "\"").Replace("{versionHash}", versionHash);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(acknowledgePurchaseOrderUrl, content);
                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("AcknowledgeOrder purchaseOrderId=" + purchaseOrderId + " status=" + statusCode + " reason=" + response.ReasonPhrase);

                    if (statusCode != 200)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        await log.WriteLineAsync("AcknowledgeOrder response body=" + responseString);
                    }
                }
            }
        }
    }
}
