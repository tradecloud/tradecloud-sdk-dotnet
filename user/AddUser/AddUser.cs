using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class AddUser
    {   
        // Add user add url
        //  https://swagger-ui.s.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user/private/specs.yaml#/user/addUserRoute
        const string addUserUrl = "https://api.accp.tradecloud1.com/v2/user/add";

        const string accessToken = "";
                        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud add user example.");
            
            var jsonContent = File.ReadAllText(@"user.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await AddUser(accessToken);

            async Task AddUser(string accessToken)
            {                                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(addUserUrl, content);
                watch.Stop();
                Console.WriteLine("AddUser StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("AddUser Body: " +  responseString);  
            }
        }
    }
}
