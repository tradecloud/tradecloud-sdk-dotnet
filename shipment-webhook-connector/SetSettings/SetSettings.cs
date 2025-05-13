using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{   
    class SetShipmentSettings
    {
        const string accessToken = "";
        const string companyId = "";
        const string body = "settings.json";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/shipment-webhook-connector/private/specs.yaml#/shipment-webhook-connector/upsertCompanyIntegrationSettings
        const string settingsUrlTemplate = "https://api.accp.tradecloud1.com/v2/shipment-webhook-connector/company/{companyId}/settings";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud Shipment Webhook Connector set settings example.");

            var settingsUrl = settingsUrlTemplate.Replace("{companyId}", companyId);
            var jsonContent = File.ReadAllText(@body);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await SetShipmentSettings();

            async Task SetShipmentSettings()
            {
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var started = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(settingsUrl, stringContent);
                watch.Stop();
                Console.WriteLine("SetShipmentEventsSettings started: " + started + " elapsed: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SetShipmentEventsSettings response StatusCode: " + (int)response.StatusCode + ", body: " + responseString);
            }
        }
    }
}
