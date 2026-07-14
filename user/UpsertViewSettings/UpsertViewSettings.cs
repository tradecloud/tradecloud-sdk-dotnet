using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class UpsertViewSettings
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";
        // Add user add url
        const string upsertViewSettingsUrl = "https://api.accp.tradecloud1.com/v2/user/view-settings";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud upsert view settings example.");

            var jsonContent = File.ReadAllText(@"view-settings.json");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await UpsertViewSettings(accessToken);

            async Task UpsertViewSettings(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(upsertViewSettingsUrl, content);
                watch.Stop();
                Console.WriteLine("UpsertViewSettings StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("UpsertViewSettings Body: " + responseString);
            }
        }
    }
}
