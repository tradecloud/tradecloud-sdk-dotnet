using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/addOrUpdatePurchaseOrder
        const string sendOrderUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrder";    

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order example.");
            var jsonContent = File.ReadAllText(@"order.json");
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            await SendOrder();

            async Task SendOrder()
            {                
                var response = await httpClient.PostAsync(sendOrderUrl, content);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ConfirmOrderLine status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("ConfirmOrderLine response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("ConfirmOrderLine response body=" +  responseString);
            }
        }
    }
}
