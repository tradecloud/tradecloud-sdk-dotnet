using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class FindCompanyById
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://tc-7612-delivery-position-migration.test.tradecloud1.com/v2/authentication/";

        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/company/internal/specs.yaml#/company/findCompanyByIdRoute
        const string findCompanyById = "https://tc-7612-delivery-position-migration.test.tradecloud1.com/v2/company/<companyId>";              

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud find company by id example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

            await FindCompanyById();

            async Task FindCompanyById()
            {                
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.GetAsync(findCompanyById);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("FindCompanyById start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("FindCompanyById response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("FindCompanyById response body=" +  responseString);
            }
        }
    }
}
