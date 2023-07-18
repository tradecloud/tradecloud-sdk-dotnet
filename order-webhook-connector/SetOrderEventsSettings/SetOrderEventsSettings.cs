using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SetOrderEventsSettings
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";

        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-webhook-connector/private/specs.yaml#/order-webhook-connector/upsertCompanyOrderEventsIntegrationSettings
        const string settingsUrl = "https://api.accp.tradecloud1.com/v2/order-webhook-connector/company/f56aa4ce-8ec8-5197-bc26-77716a58add7/settings/orderEvents";   

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud Order Webhook Connector set order events settings example.");

            var jsonContent = File.ReadAllText(@"order-events-settings.json");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await SetOrderEventsSettings();

            async Task SetOrderEventsSettings()
            {                
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var started = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(settingsUrl, stringContent);
                watch.Stop();
                Console.WriteLine("SetOrderEventsSettings started: " + started +  " elapsed: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SetOrderEventsSettings response StatusCode: " + (int)response.StatusCode + ", body: " +  responseString);  
            }
        }
    }
}
