using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class AddIdentity
    {   
        // Add identity add url
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/private/specs.yaml#/authentication/add
        const string addIdentityUrl = "https://api.accp.tradecloud1.com/v2/authentication/add";

        const string accessToken = "";
                        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud add identity example.");
            
            var jsonContent = File.ReadAllText(@"identity.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await AddIdentity(accessToken);

            async Task AddIdentity(string accessToken)
            {                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(addIdentityUrl, content);
                watch.Stop();
                Console.WriteLine("AddIdentity StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("AddIdentity Body: " +  responseString);  
            }
        }
    }
}
