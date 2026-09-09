using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchUsers
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/shipment/specs.yaml#/shipment/searchShipmentsRoute
        const string searchShipmentsUrl = "https://api.accp.tradecloud1.com/v2/shipment/search";
        static async Task Main(string[] args)
        {
            EnvFile.Load();
            if (!EnvFile.TryUsernamePassword(out var username, out var password))
                return;

            Console.WriteLine("Tradecloud search shipments example.");

            var jsonContent = File.ReadAllText(@"search-shipment.json");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, _) = await authenticationClient.Login(username, password);
            await SearchShipments(accessToken);

            async Task SearchShipments(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(searchShipmentsUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchShipments start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("SearchShipments request body=" + jsonContent);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    // Write the response to a JSON file
                    string fileName = "shipments.json";
                    File.WriteAllText(fileName, JValue.Parse(responseString).ToString(Formatting.Indented));
                    Console.WriteLine($"SearchShipments response written to {fileName}");
                }
                else
                    Console.WriteLine("SearchShipments response body=" + responseString);
            }
        }
    }
}