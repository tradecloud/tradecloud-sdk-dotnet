using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ResendOrder
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";
        const string baseUrl = "https://accp.tradecloud.nl/api/v1";    
        const string purchaseOrderCode = "";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud resend order.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            
            
            var id = await getOrderId(purchaseOrderCode);
            await resendOrder(purchaseOrderCode, id);
                        
            async Task<string> getOrderId(string purchaseOrderCode)
            {
                var getOrderIdUrl = baseUrl + "/purchaseOrder/code/" + purchaseOrderCode;
                var response = await httpClient.GetAsync(getOrderIdUrl);
                var statusCode = (int)response.StatusCode;
                string id = null;
                if (statusCode == 200) 
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    dynamic json = JsonConvert.DeserializeObject<dynamic>(result);
                    id = json.id;
                    //Console.WriteLine("getOrderId code=" + code + " id=" +  id + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                } 
                else 
                {
                    Console.WriteLine("getOrderId code=" + purchaseOrderCode + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                } 
                return id;
            }

            async Task resendOrder(string code, string id)
            {                
                var resendOrderUrl = baseUrl + "/purchaseOrder/" + id + "/_resend";
                var response = await httpClient.PostAsync(resendOrderUrl, null);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("resendOrder code=" + purchaseOrderCode + " id=" +  id + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                // string responseString = await response.Content.ReadAsStringAsync();
                // if (statusCode == 200)
                //     Console.WriteLine("ConfirmOrderLine response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                // else
                //     Console.WriteLine("ConfirmOrderLine response body=" +  responseString);
            }
        }
    }
}
