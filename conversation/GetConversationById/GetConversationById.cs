using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class GetConversationById
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/conversation/private/specs.yaml#/conversation/getConversationById
        // Fill in manadatory user id
        const string getConversationByIdUrl = "https://api.accp.tradecloud1.com/v2/conversation/order/f56aa4ce-8ec8-5197-bc26-77716a58add7-PO-05-%2351388860";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud get conversation by id example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken)  = await authenticationClient.Login(username, password);
            await GetConversationById(accessToken);

            async Task GetConversationById(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.GetAsync(getConversationByIdUrl);
                watch.Stop();
                                
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrder start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("GetConversationById response content=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("GetConversationById response content=" + responseString);
            }
        }
    }
}

