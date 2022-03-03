using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchUsers
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user-search/private/specs.yaml#/user-search/userSearchRoute
        const string getUserByEmailUrl = "https://api.accp.tradecloud1.com/v2/user-search/search";

        const string jsonContentWithSingleQuotes = 
            @"{
               `companyId`: `<companyId>`,
               `query`: `<query>`,
               `role`: `<role>`,
               `offset`: 0,
               `limit`: 100
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud search users example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken)  = await authenticationClient.Login(username, password);
            await SearchUsers(accessToken);

            async Task SearchUsers(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(getUserByEmailUrl, content);
                watch.Stop();
                Console.WriteLine("SearchUsers StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SearchUsers Body: " +  responseString);  
            }
        }
    }
}
