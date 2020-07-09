using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class AddIdentity
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory  password
        const string password = "";
        // Add identity add url
        const string addIdentityUrl = "";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = 
            @"{
                `email`: `user@example.com`,
                `plainPassword`: ``,
                `userId`: `40fef20d-8769-4a0b-aa2d-90a0b00750b5`,
                `userRoles`: [
                    `buyer`
                ],
                `companyId`: `f56aa4ce-8ec8-5197-bc26-77716a58add7`
            }";
                        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud add identity example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await AddIdentity(accessToken);

            async Task AddIdentity(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
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
