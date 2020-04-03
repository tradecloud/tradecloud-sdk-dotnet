using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class GetOrderById
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/login";
        // Fill in mandatory username
        const string username = "frankjan@tradecloud1.com";
        // Fill in mandatory password
        const string password = "SecretSecret1";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/";
        // Fill in manadatory order id
        const string orderId = "f56aa4ce-8ec8-5197-bc26-77716a58add7-15-16342242";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud get order by id example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var token = await authenticationClient.Authenticate(username, password);
            await GetOrderById(token);

            async Task GetOrderById(string token)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await httpClient.GetAsync(orderSearchUrl + orderId);

                Console.WriteLine("GetOrderById StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GetOrderById Content: " +  JValue.Parse(responseString).ToString(Formatting.Indented));  
            }
        }
    }
}
