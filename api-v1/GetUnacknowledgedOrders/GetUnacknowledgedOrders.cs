using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class GetUnacknowledgedOrders
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/listPurchaseOrders
        const string getUnacknowledgedOrdersUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrder?acknowledged=false&limit=100";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud get unacknowledged orders example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            await GetUnacknowledgedOrders();

            async Task GetUnacknowledgedOrders()
            {                
                var response = await httpClient.GetAsync(getUnacknowledgedOrdersUrl);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("GetUnacknowledgedOrders status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("GetUnacknowledgedOrders response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("GetUnacknowledgedOrders response body=" +  responseString);
            }
        }
    }
}
