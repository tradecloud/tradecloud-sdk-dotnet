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

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user-search/specs.yaml#/user-search/findUserByIdRoute
        // Fill in manadatory user id
        const string findUserByIdUrl = "https://api.accp.tradecloud1.com/v2/user-search/<id>";

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
                
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.GetAsync(findUserByIdUrl);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("FindUserById start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("FindUserById response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("FindUserById response body=" +  responseString);
            }
        }
    }
}
