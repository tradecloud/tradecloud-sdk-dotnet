using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CreateConversation
    {   
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/conversation/private/specs.yaml#/conversation/createOrderConversation
        const string createConversationUrl = "https://api.accp.tradecloud1.com/v2/conversation/order/create";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud create conversation.");

            var jsonContent = File.ReadAllText(@"conversation.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            
            await CreateConversation();

            async Task CreateConversation()
            {                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(createConversationUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("CreateConversation start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("CreateConversation request body=" + jsonContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("CreateConversation response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("CreateConversation response body=" +  responseString);
            }
        }
    }
}
