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
    class PollSingleDeliveryOrders
    {
        const bool useToken = true;
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";

        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search/pollOrdersSingleDeliveryRoute
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/poll/single-delivery";

        // Output file path
        const string outputFilePath = "single-delivery-orders.json";
        
        // Page size for each request
        const int pageSize = 100;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud poll single delivery orders example.");

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
            await PollAllSingleDeliveryOrders(httpClient);
        }

        static async Task PollAllSingleDeliveryOrders(HttpClient httpClient)
        {
            Console.WriteLine("Starting to poll all single delivery orders...");
            
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
                Console.WriteLine($"page={offset/pageSize + 1} start={start} elapsed={watch.ElapsedMilliseconds}ms status={statusCode} reason={response.ReasonPhrase}");
                Console.WriteLine($"jsonContent={jsonContent}");
                
                if (statusCode != 200)
                {
                    Console.WriteLine($"Error fetching page with offset={offset} reason={response.ReasonPhrase}");
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response={errorResponse}");
                    break;
                }
                
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JObject.Parse(responseString);
                
                // Extract orders from response - now using "data" instead of "orders"
                var orders = responseObj["data"] as JArray;
                
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
                    ["companyId"] = "5e94d46c-df68-46a2-8cb6-6d176ca51b56",
                    ["lastUpdatedAfter"] = "2025-05-12T00:00:00.000+02:00"
                },
                ["offset"] = offset,
                ["limit"] = limit
            };
            
            return requestObj.ToString();
        }
    }
}
