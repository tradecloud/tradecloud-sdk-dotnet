using System;
using System.IO;
using System.Globalization;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder
    {   
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendForecastByBuyerRoute
        const string sendForecastUrl = "https://api.accp.tradecloud1.com/v2/api-connector/forecast";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud seed forecasts.");

            var jsonForecastTemplate = File.ReadAllText(@"forecast.json");
            var jsonLineTemplate = File.ReadAllText(@"line.json");            

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

            var rnd = new Random();
            var forecastNumber = "Forecast" + rnd.Next(100000, 999999);
            var jsonForecastWithForecastNumber = jsonForecastTemplate.Replace("{forecastNumber}", forecastNumber);
            Console.WriteLine("Starting, forecastNumber=" + forecastNumber);
            
            for (int item = 0; item < 100; item++) 
            {
                string jsonLines = "";
                var buyerItemNumber = rnd.Next(100000, 999999);
                var jsonLine = jsonLineTemplate.Replace("{buyerItemNumber}", "Item" + buyerItemNumber + item);
                var lastMonth = 12;
                for (int month = 1; month <= lastMonth; month++) 
                {
                    var noPerMonth = rnd.Next(1, 3);
                    for (int no = 1; no <= noPerMonth; no++) 
                    {
                        var startDate = new DateTime(2023, month, 1).ToString("yyyy-MM-dd");
                        var endDate = new DateTime(2023, month, 1).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
                        var quantity = rnd.Next(1, 9999).ToString() + "." + rnd.Next(1, 99);
                        jsonLines += jsonLine.Replace("{startDate}", startDate).Replace("{endDate}", endDate).Replace("{quantity}", quantity);
                        if (month < lastMonth)
                        {
                            jsonLines += ",";
                        }
                    }
                }
                var jsonForecastWithLines = jsonForecastWithForecastNumber.Replace("{lines}", jsonLines);

                var issueDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                var jsonForecastWithIssueDateTime = jsonForecastWithLines.Replace("{issueDateTime}", issueDateTime);

                Console.WriteLine("item=" + item + " json=" + jsonForecastWithIssueDateTime);
                await SendForecast(jsonForecastWithIssueDateTime);
            }
            Console.WriteLine("Finished, forecastNumber=" + forecastNumber);

            double GetDoubleWithinRange(System.Random rnd, double lowerBound, double upperBound)
            {
                var rDouble = rnd.NextDouble();
                var rRangeDouble = (double) rDouble * (upperBound - lowerBound) + lowerBound;
                return rRangeDouble;
            }

            async Task SendForecast(string jsonContent)
            {                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendForecastUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendForecast start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SendForecast request body=" + jsonContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("SendForecast response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("SendForecast response body=" +  responseString);
            }
        }
    }
}
