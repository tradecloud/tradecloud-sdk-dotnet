using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ExportConnections
    {
        const string accessToken = "";
        const string companyId = "";
        const string fileName = "connections-export.csv";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/company/private/specs.yaml#/company/searchConnectionsRoute
        const string connectionSearchUrlTemplate = "https://api.accp.tradecloud1.com/v2/company/{companyId}/connection/search";
        const int limit = 100;
        const int maxTotal = 10000;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud export connections example.");

            var jsonContentTemplate = File.ReadAllText(@"connections-search-template.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var csvWriter = new StreamWriter(fileName, append: false))
            {
                // Write CSV header
                await csvWriter.WriteLineAsync("companyName,accountNumber");

                int offset = 0;
                int total = limit;
                while (total > offset && offset < maxTotal)
                {
                    var queryResult = await SearchConnections(offset);
                    if (queryResult != null)
                    {
                        total = (int)queryResult["total"];
                        Console.WriteLine("total=" + total + " offset=" + offset);
                        offset += limit;

                        foreach (var connection in queryResult.First.Values())
                        {
                            string companyName, accountCode;
                            string requestingCompanyId = connection["requestingCompanyId"].ToString();
                            if (requestingCompanyId == companyId)
                            {
                                companyName = connection["acceptingCompanyName"].ToString();
                                accountCode = connection["requestingCompanyAccountCode"]?.ToString() ?? "";
                            }
                            else
                            {
                                companyName = connection["requestingCompanyName"].ToString();
                                accountCode = connection["acceptingCompanyAccountCode"]?.ToString() ?? "";
                            }
                            //Console.WriteLine("companyName=" + companyName + " accountCode=" + accountCode);

                            // Escape quotes in company name and wrap fields in quotes
                            companyName = $"\"{companyName.Replace("\"", "\"\"")}\"";
                            accountCode = $"\"{accountCode.Replace("\"", "\"\"")}\"";

                            // Write to CSV
                            await csvWriter.WriteLineAsync($"{companyName},{accountCode}");
                        }
                    }
                }
            }

            async Task<JObject> SearchConnections(int offset)
            {
                var url = connectionSearchUrlTemplate.Replace("{companyId}", companyId);
                var jsonContent = jsonContentTemplate.Replace("{offset}", offset.ToString());
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(url, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchConnections start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("SearchConnections request body=" + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    Console.WriteLine("SearchOrders response body=" + responseString);
                    return null;
                }
            }
        }
    }
}
