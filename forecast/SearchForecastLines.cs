using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchForecastLines
    {   
        const bool useToken = true;
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string forecastLineSearchUrl = "https://api.accp.tradecloud1.com/v2/forecast/line/search";
        
        // Fill in the search query
        const string jsonContentWithSingleQuotes = @"{
            'queries': {
                'forecastNumber': 'F123456789',
                'buyerItemNumber': '12345',
                'supplierItemNumber': '67890'
            },
            'filters': {
                'buyerForecast': {
                    'companyId': 'f56aa4ce-8ec8-5197-bc26-77716a58add7'
                },
                'supplierForecast': {
                    'companyId': '1f61e695-7545-5670-9384-58e8b1f263e6'
                }
            },
            'sort': [
                {
                'field': 'buyerLine.item.number',
                'order': 'asc'
                }
            ],
            'offset': 0,
            'limit': 10
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud search forecast lines example.");
            
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
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            }
            await SearchForecastLines();

            async Task SearchForecastLines()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(forecastLineSearchUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchForecastLines start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SearchForecastLines request body=" + jsonContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("SearchForecastLines response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("SearchForecastLines response body=" +  responseString);
            }
        }
    }
}
