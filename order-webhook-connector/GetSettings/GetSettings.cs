using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class GetSettings
    {
        const string accessToken = "";
        const string companyId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-webhook-connector/private/specs.yaml#/order-webhook-connector/getCompanyIntegrationSettings
        const string settingsUrlTemplate = "https://api.accp.tradecloud1.com/v2/order-webhook-connector/company/{companyId}/settings";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud Order Webhook Connector get settings example.");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await GetSettings();

            async Task GetSettings()
            {
                var settingsUrl = settingsUrlTemplate.Replace("{companyId}", companyId);

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.GetAsync(settingsUrl);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("GetSettings start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("GetSettings response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("GetSettings response body=" + responseString);
            }
        }
    }
}
