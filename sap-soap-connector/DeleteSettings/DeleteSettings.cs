using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class GetSettings
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://tc-7853-sap-soap-config.t.tradecloud1.com/v2/authentication/";

        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/sap-soap-connector/private/specs.yaml#/sap-soap-connector/deleteSapSettings
        const string settingsUrl = "https://tc-7853-sap-soap-config.t.tradecloud1.com/v2/sap-soap-connector/company/<companyId>/settings";   

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud SAP SOAP Connector delete settings example.");
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await GetSettings();

            async Task GetSettings()
            {                
                var started = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.DeleteAsync(settingsUrl);
                watch.Stop();
                Console.WriteLine("DeleteSettings started: " + started +  " elapsed: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("DeleteSettings response StatusCode: " + (int)response.StatusCode + ", body: " +  responseString);  
            }
        }
    }
}
