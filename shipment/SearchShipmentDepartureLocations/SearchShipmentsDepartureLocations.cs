using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchShipmentDepartureLocations
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/shipment/specs.yaml#/shipment/searchShipmentsRoute
        const string url = "https://api.accp.tradecloud1.com/v2/shipment/departure/search";

        static async Task Main()
        {
            Console.WriteLine("Tradecloud search shipment departure locations example.");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, _) = await authenticationClient.Login(username, password);
            await SearchShipmentDepartureLocations(accessToken);

            async Task SearchShipmentDepartureLocations(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var jsonContent = File.ReadAllText(@"search.json");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(url, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchShipmentDepartureLocations start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    // Write the response to a JSON file
                    string fileName = "shipment_departure_locations.json";
                    File.WriteAllText(fileName, JValue.Parse(responseString).ToString(Formatting.Indented));
                    Console.WriteLine($"SearchShipmentDepartureLocations response written to {fileName}");
                }
                else
                    Console.WriteLine("SearchShipmentDepartureLocations response body=" + responseString);
            }
        }
    }
}