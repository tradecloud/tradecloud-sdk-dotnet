using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ListDestinations
    {
        const string fileName = "destinations.json";
        const bool useToken = true;
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/private/specs.yaml#/order-search/destinationsRoute
        const string destinationsUrl = "https://api.accp.tradecloud1.com/v2/order-search/destinations";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud list destinations example.");

            var jsonContent = File.ReadAllText(@fileName);

            HttpClient httpClient = new HttpClient();
            if (useToken)
            {
                var authenticationClient = new Authentication(httpClient, authenticationUrl);
                var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
            else
            {
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);
            }
            await ListDestinations();

            async Task ListDestinations()
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(destinationsUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ListDestinations start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("ListDestinations request body=" + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("ListDestinations response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("ListDestinations response body=" + responseString);
            }
        }
    }
}
