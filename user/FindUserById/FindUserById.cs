using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class FindUserById
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user/specs.yaml#/user/findUserByIdRoute
        // Fill in manadatory user id
        const string findUserByIdUrl = "https://api.accp.tradecloud1.com/v2/user/<id>";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud find user by id example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken)  = await authenticationClient.Login(username, password);
            await findUserByIdRequest(accessToken);

            async Task findUserByIdRequest(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.GetAsync(findUserByIdUrl);

                Console.WriteLine("FindUserById StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("FindUserById Content: " +  JValue.Parse(responseString).ToString(Formatting.Indented));   
            }
        }
    }
}
