using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class UpdateUserRoles
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";
        // Add user add url
        const string updateUserRolesUrl = "";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = 
            @"{
                `roles`: [
                    `buyer`
                ]
            }";
                        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud update user roles example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await UpdateUserRoles(accessToken);

            async Task UpdateUserRoles(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PutAsync(updateUserRolesUrl, content);
                watch.Stop();
                Console.WriteLine("UpdateUserRoles StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("UpdateUserRoles Body: " +  responseString);  
            }
        }
    }
}
