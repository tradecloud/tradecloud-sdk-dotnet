using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class MigrateCompany
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/migration/private/specs.yaml#/migration/PurchaseOrderUpdated
        const string migrateCompanyUrl = "https://api.accp.tradecloud1.com/v2/migration/company";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud company migrate example.");

            var jsonContent = File.ReadAllText(@"company.json");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await MigrateCompany();

            async Task MigrateCompany()
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(migrateCompanyUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("MigrateCompany start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("MigrateCompany request body=" + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("MigrateCompany response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("MigrateCompany response body=" + responseString);
            }
        }
    }
}
