using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ArchiveOrderLine
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // Fill in the order LINE id (UUID) you got when fetching the order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/archivePurchaseOrderLine
        const string archivePurchaseOrderLineUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrderLine/{purchaseOrderLineId}";    

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud archive order line example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            await ArchiveOrderLine();

            async Task ArchiveOrderLine()
            {                
                var response = await httpClient.DeleteAsync(archivePurchaseOrderLineUrl);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ArchiveOrderLine status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("ArchiveOrderLine response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("ArchiveOrderLine response body=" +  responseString);
            }
        }
    }
}
