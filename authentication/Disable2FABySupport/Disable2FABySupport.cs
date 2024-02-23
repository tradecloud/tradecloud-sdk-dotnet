using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class Disable2FABySupport
    {
        // Support or superuser role required
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/private/specs.yaml#/authentication/disable2FABySupport
        const string disable2FABySupportURL = "https://api.accp.tradecloud1.com/v2/authentication/2fa/disable/support";

        const string jsonContentWithSingleQuotes =
            @"{
               `email`: ``
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud disable 2FA by support.");

            HttpClient httpClient = new HttpClient();
            await Disable2FABySupport(accessToken);

            async Task Disable2FABySupport(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(disable2FABySupportURL, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("Disable2FABySupport start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("Disable2FABySupport response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("Disable2FABySupport response body=" + responseString);
            }
        }
    }
}
