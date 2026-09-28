using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class Disable2FAByOther
    {
        // Admin (same company) or support/superuser (another company). Never the caller's own 2FA.
        // 200 is { "ok": true }. The response does not replace the caller's session.
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/private/specs.yaml#/authentication/disable2FaByOther
        const string disable2FAByOtherURL = "https://api.accp.tradecloud1.com/v2/authentication/2fa/disable/other";

        const string jsonContentWithSingleQuotes =
            @"{
               `email`: ``
            }";

        static async Task Main(string[] args)
        {
            EnvFile.Load();
            if (!EnvFile.TryAccessToken(out var accessToken))
                return;

            Console.WriteLine("Tradecloud disable 2FA for another user.");

            HttpClient httpClient = new HttpClient();
            await Disable2FAByOtherRequest(accessToken);

            async Task Disable2FAByOtherRequest(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(disable2FAByOtherURL, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("Disable2FAByOther start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("Disable2FAByOther response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("Disable2FAByOther response body=" + responseString);
            }
        }
    }
}
