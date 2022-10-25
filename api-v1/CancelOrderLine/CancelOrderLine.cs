using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CancelOrderLine
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // Fill in the order LINE id (UUID) you got when fetching the order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/cancelPurchaseOrderLineById
        const string cancelPurchaseOrderLineUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrderLine/{purchaseOrderLineId}/_cancel";
        
        const string jsonContentWithSingleQuotes = @"{
            'explanation': '<explanation>'
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud cancel order line example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            await CancelOrderLine();

            async Task CancelOrderLine()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(cancelPurchaseOrderLineUrl, content);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("CancelOrderLine status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("CancelOrderLine response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("CancelOrderLine response body=" +  responseString);
            }
        }
    }
}
