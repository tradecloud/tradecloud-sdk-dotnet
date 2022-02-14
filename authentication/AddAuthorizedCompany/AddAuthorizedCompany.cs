using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class AddAuthorizedCompany
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory  password
        const string password = "";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = 
            @"{
                `email`: ``,
                `companyId`: ``
            }";
                        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud add authorized company example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await AddAuthorizedCompany(accessToken);

            async Task AddAuthorizedCompany(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(authenticationUrl + "authorization", content);
                watch.Stop();
                Console.WriteLine("AddAuthorizedCompany StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("AddAuthorizedCompany Body: " +  responseString);  
            }
        }
    }
}
