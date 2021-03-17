using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SetSettings
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";

        const string username = ";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/sci-connector/specs.yaml#/sci-connector/upsertSciApiUrl
        const string setUrlUrl = "https://api.accp.tradecloud1.com/v2/sci-connector/company/{buyerCompanyId}/settings/url";   

        const string urlContent = @"{
            `url`: `https://sci.mycompany.com/api`
        }";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/sci-connector/specs.yaml#/sci-connector/uspertSupplierCredentials
        const string setCredentialsUrl = "https://api.accp.tradecloud1.com/v2/sci-connector/company/{buyerCompanyId}/settings/{supplierCompanyId}/credentials";
        const string credentialsContent = @"{
            `username`: `SUPP123`,
            `password`: `ReallyStrongPassword123`
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud SCI Connector set settings example.");
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await SetUrl();
            await SetCredentials();

            async Task SetUrl()
            {                
                var jsonContent = urlContent.Replace("`", "\"");
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var started = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(setUrlUrl, stringContent);
                watch.Stop();
                Console.WriteLine("SetUrl started: " + started +  " elapsed: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SetUrl response StatusCode: " + (int)response.StatusCode + ", body: " +  responseString);  
            }

            async Task SetCredentials()
            {                
                var jsonContent = credentialsContent.Replace("`", "\"");
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var started = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(setCredentialsUrl, stringContent);
                watch.Stop();
                Console.WriteLine("SetCredentials started: " + started +  " elapsed: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SetCredentials response StatusCode: " + (int)response.StatusCode + ", body: " +  responseString);  
            }
        }
    }
}
