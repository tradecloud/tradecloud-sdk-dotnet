using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

namespace Com.Tradecloud1.SDK.Client
{
    class PollOrders
    {
        const bool useToken = true;
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login

        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search/pollOrdersRoute
        const string orderSearchUrl = "https://api.tradecloud1.com/v2/order-search/poll";

        // Output file path
        const string outputFilePath = "orders.json";

        // Page size for each request
        const int pageSize = 1000;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud poll orders example.");

            HttpClient httpClient = new HttpClient();
            if (useToken)
            {
                //var authenticationClient = new Authentication(httpClient, authenticationUrl);
                //var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
                var accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJkYXRhIjp7InVzZXJuYW1lIjoibWFyY2VsQHRyYWRlY2xvdWQxLmNvbSIsInVzZXJJZCI6ImYxY2YzNDA0LTMxMTktNDllYi05NGNlLTkxYWU0ZTY1NTc5ZCIsInVzZXJSb2xlcyI6WyJzdXBwb3J0Il0sImNvbXBhbnlSb2xlcyI6W10sImF1dGhvcml6ZWRDb21wYW55SWRzIjpbXSwiY29tcGFueUlkIjoiMDY4OTNiYmEtZTEzMS00MjY4LTg3YzktN2ZhZTY0ZTE2ZWU5IiwidHdvRkFFbmFibGVkIjp0cnVlLCJ0d29GQUVuZm9yY2VkIjp0cnVlLCJzdGF0dXMiOiJhdXRoZW50aWNhdGVkIiwiaWRlbnRpdHlQcm92aWRlciI6InRyYWRlY2xvdWQifSwiZXhwIjoxNzQ4MjY1MjYwfQ.5cO4kHAe0moH0umoTbpcKmb-VOjnngf7blMgiwESACI";
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
            else
            {
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);
            }
            await PollAllOrders(httpClient);
        }

        static async Task PollAllOrders(HttpClient httpClient)
        {
            Console.WriteLine("Starting to poll all orders...");

            // List to store all orders
            var allOrders = new List<JObject>();

            // Start with offset 0
            int offset = 0;
            bool hasMoreResults = true;
            int totalFetched = 0;
            DateTime? lastUpdatedAt = null;

            while (hasMoreResults)
            {
                // Create request with current offset
                var jsonContent = CreateJsonRequest(offset, pageSize);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(orderSearchUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine($"page={offset / pageSize + 1} start={start} elapsed={watch.ElapsedMilliseconds}ms status={statusCode} reason={response.ReasonPhrase}");

                if (statusCode == 400)
                    Console.WriteLine($"Request body={jsonContent}");

                if (statusCode != 200)
                {
                    Console.WriteLine($"Error fetching page with offset={offset} reason={response.ReasonPhrase}");
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response={errorResponse}");
                    break;
                }

                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JObject.Parse(responseString);

                // Extract orders from response - check both "data" and "orders" for compatibility
                var orders = responseObj["data"] as JArray ?? responseObj["orders"] as JArray;

                if (orders != null && orders.Count > 0)
                {
                    // Add orders to the collection
                    foreach (JObject order in orders)
                    {
                        allOrders.Add(order);
                    }

                    totalFetched += orders.Count;
                    Console.WriteLine($"Fetched={orders.Count} total={totalFetched}");

                    // Update lastUpdatedAt if available
                    if (responseObj["lastUpdatedAt"] != null)
                    {
                        lastUpdatedAt = responseObj["lastUpdatedAt"].ToObject<DateTime>();
                    }

                    // Check if we need to fetch more pages
                    if (orders.Count < pageSize)
                    {
                        hasMoreResults = false;
                    }
                    else
                    {
                        // Move to next page
                        offset += pageSize;
                    }
                }
                else
                {
                    // No more results
                    hasMoreResults = false;
                }
            }

            Console.WriteLine($"Finished fetching all orders. Total={totalFetched}");

            // Save all orders to a single JSON file
            var resultObject = new JObject
            {
                ["data"] = new JArray(allOrders),
                ["total"] = totalFetched
            };

            // Add lastUpdatedAt if available
            if (lastUpdatedAt.HasValue)
            {
                resultObject["lastUpdatedAt"] = JToken.FromObject(lastUpdatedAt.Value);
            }

            File.WriteAllText(outputFilePath, resultObject.ToString(Formatting.Indented));
            Console.WriteLine($"All orders saved to {outputFilePath}");
        }

        static string CreateJsonRequest(int offset, int limit)
        {
            var requestObj = new JObject
            {
                ["filters"] = new JObject
                {
                    ["companyId"] = "104f55fb-4e7e-40e2-87d6-7c901d241931",
                    ["lastUpdatedAfter"] = "2025-05-01T00:00:00Z"
                },
                ["offset"] = offset,
                ["limit"] = limit
            };

            return requestObj.ToString();
        }
    }
}
