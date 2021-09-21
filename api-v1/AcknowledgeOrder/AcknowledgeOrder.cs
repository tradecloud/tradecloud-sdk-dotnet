using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class AcknowledgeOrder
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // Fill in the order id (UUID) you got when fetching the order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/acknowledgePurchaseOrder
        const string acknowledgePurchaseOrderUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrder/{purchaseOrderId}/_acknowledge";
        
        // Fill in the versionHash you got when fetching the order
        const string jsonContentWithSingleQuotes = @"{
            'versionHash': '{versionHash}'
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud acknowledge order example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            await AcknowledgeOrder();

            async Task AcknowledgeOrder()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(acknowledgePurchaseOrderUrl, content);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("AcknowledgeOrder status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("AcknowledgeOrder response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("AcknowledgeOrder response body=" +  responseString);
            }
        }
    }
}
