using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SetOrderDocumentsEventsSettings
    {   
       const string companyId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
    
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.test.tradecloud1.com/?url=https://tc-9116-webhook-xml-request.t.tradecloud1.com/v2/order-webhook-connector/private/specs.yaml#/order-webhook-connector/upsertCompanyOrderDocumentsEventIntegrationSettings
        const string settingsUrlTemplate = "https://api.accp.tradecloud1.com/v2/order-webhook-connector/company/{companyId}/settings/orderDocumentsEvents";           

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud Order Webhook Connector set order documents events settings example.");

            var jsonContent = File.ReadAllText(@"order-documents-events-settings.json");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await SetSettings();

            async Task SetSettings()
            {                
                var settingsUrl = settingsUrlTemplate.Replace("{companyId}", companyId);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var started = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(settingsUrl, stringContent);
                watch.Stop();
                Console.WriteLine("SetOrderEventSettings started: " + started +  " elapsed: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SetOrderEventSettings response StatusCode: " + (int)response.StatusCode + ", body: " +  responseString);  
            }
        }
    }
}
