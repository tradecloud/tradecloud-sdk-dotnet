using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class FindIdentityByEmail
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
       const string username = "";
        // Fill in mandatory password
        const string password = "";
  
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/internal/specs.yaml#/authentication/findIdentityByEmail
        const string getUserByEmailUrl = "https://api.tradecloud1.com/v2/authentication/find";

        const string jsonContentWithSingleQuotes = 
            @"{
               `email`: `commercial@comotech.fr`
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud find identity by email example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            //var (accessToken, refreshToken)  = await authenticationClient.Login(username, password);
            var accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJkYXRhIjp7InVzZXJuYW1lIjoibWFyY2VsQHRyYWRlY2xvdWQxLmNvbSIsInVzZXJJZCI6ImYxY2YzNDA0LTMxMTktNDllYi05NGNlLTkxYWU0ZTY1NTc5ZCIsInVzZXJSb2xlcyI6WyJzdXBwb3J0Il0sImNvbXBhbnlSb2xlcyI6W10sImF1dGhvcml6ZWRDb21wYW55SWRzIjpbXSwiY29tcGFueUlkIjoiMDY4OTNiYmEtZTEzMS00MjY4LTg3YzktN2ZhZTY0ZTE2ZWU5IiwidHdvRkFFbmFibGVkIjp0cnVlLCJ0d29GQUVuZm9yY2VkIjp0cnVlLCJzdGF0dXMiOiJhdXRoZW50aWNhdGVkIiwiaWRlbnRpdHlQcm92aWRlciI6InRyYWRlY2xvdWQifSwiZXhwIjoxNjg2NzU5MDIzfQ.l7yUNCa6WjHdLjVIaLYjRNn2E5qAW_n-MAlxW30n0bo";
            await FindIdentityByEmailRequest(accessToken);

            async Task FindIdentityByEmailRequest(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(getUserByEmailUrl, content);
                watch.Stop();
                Console.WriteLine("FindIdentityByEmail StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("FindIdentityByEmail Body: " +  JValue.Parse(responseString).ToString(Formatting.Indented)); 
            }
        }
    }
}
