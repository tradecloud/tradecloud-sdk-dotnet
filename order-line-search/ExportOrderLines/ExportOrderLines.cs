using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ExportOrderLines
    {
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-line-search/private/specs.yaml#/order-line-search/searchRoute
        const string orderLineSearchUrl = "https://api.accp.tradecloud1.com/v2/order-line-search/search";
        const string outputCsvFile = "order_lines_export.csv";
        const int pageSize = 100;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud export order lines example.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await ExportOrderLinesToCsv();

            Console.WriteLine($"Export completed. Results saved to {outputCsvFile}");
        }

        static async Task ExportOrderLinesToCsv()
        {
            var allOrderLines = new List<Dictionary<string, string>>();
            var offset = 0;
            var hasMoreResults = true;
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            Console.WriteLine("Starting export of order lines...");

            while (hasMoreResults)
            {
                Console.WriteLine($"Fetching page with offset: {offset}");
                var orderLines = await GetOrderLinesPage(httpClient, offset);

                if (orderLines.Count == 0)
                {
                    hasMoreResults = false;
                    Console.WriteLine("No more results found.");
                }
                else
                {
                    allOrderLines.AddRange(orderLines);
                    Console.WriteLine($"Retrieved {orderLines.Count} order lines. Total: {allOrderLines.Count}");
                    offset += pageSize;
                }
            }

            WriteToCsvFile(allOrderLines);
        }

        static async Task<List<Dictionary<string, string>>> GetOrderLinesPage(HttpClient httpClient, int offset)
        {
            var jsonContentTemplate = File.ReadAllText("search.json");
            var jsonContent = jsonContentTemplate.Replace("{offset}", offset.ToString());
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(orderLineSearchUrl, content);

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("ExportOrderLines status=" + statusCode + " reason=" + response.ReasonPhrase);

            if (statusCode != 200)
            {
                if (statusCode == 400)
                    Console.WriteLine("ExportOrderLines request body=" + jsonContent);

                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error response body=" + errorResponse);
                return new List<Dictionary<string, string>>();
            }

            string responseString = await response.Content.ReadAsStringAsync();
            return FlattenOrderLinesJson(responseString);
        }

        static List<Dictionary<string, string>> FlattenOrderLinesJson(string json)
        {
            var result = new List<Dictionary<string, string>>();
            var jObject = JObject.Parse(json);

            Console.WriteLine("JSON Response Keys: " + string.Join(", ", jObject.Properties().Select(p => p.Name)));

            // Check for different possible array properties in order of likelihood
            JArray orderLines = null;
            string foundArrayName = "";

            // Check for paginated response structures
            if (jObject["paging"] != null && jObject["data"] != null)
            {
                orderLines = jObject["data"] as JArray;
                foundArrayName = "data (with paging)";

                // Log pagination info if available
                var paging = jObject["paging"];
                if (paging != null)
                {
                    Console.WriteLine($"Pagination info - Total: {paging["total"]}, Offset: {paging["offset"]}, Max: {paging["max"]}");
                }
            }
            else if (jObject["hits"] != null)
            {
                orderLines = jObject["hits"] as JArray;
                foundArrayName = "hits";
            }
            else if (jObject["data"] != null)
            {
                orderLines = jObject["data"] as JArray;
                foundArrayName = "data";
            }
            else if (jObject["orderLines"] != null)
            {
                orderLines = jObject["orderLines"] as JArray;
                foundArrayName = "orderLines";
            }
            else if (jObject["results"] != null)
            {
                orderLines = jObject["results"] as JArray;
                foundArrayName = "results";
            }
            else if (jObject["items"] != null)
            {
                orderLines = jObject["items"] as JArray;
                foundArrayName = "items";
            }
            else if (jObject["content"] != null)
            {
                orderLines = jObject["content"] as JArray;
                foundArrayName = "content";
            }
            else
            {
                // Try to find any array property
                var arrayProp = jObject.Properties().FirstOrDefault(p => p.Value.Type == JTokenType.Array);
                if (arrayProp != null)
                {
                    orderLines = arrayProp.Value as JArray;
                    foundArrayName = arrayProp.Name;
                }
                else
                {
                    // Check if the root itself is an array
                    var rootToken = JToken.Parse(json);
                    if (rootToken.Type == JTokenType.Array)
                    {
                        orderLines = rootToken as JArray;
                        foundArrayName = "root array";
                    }
                }
            }

            if (orderLines == null || !orderLines.Any())
            {
                Console.WriteLine($"No order lines array found in response. Available keys: {string.Join(", ", jObject.Properties().Select(p => p.Name))}");

                // Show first few characters of response for debugging
                var truncatedResponse = json.Length > 500 ? json.Substring(0, 500) + "..." : json;
                Console.WriteLine($"Response preview: {truncatedResponse}");
                return result;
            }

            Console.WriteLine($"Found array '{foundArrayName}' with {orderLines.Count} items");
            Console.WriteLine($"Processing {orderLines.Count} order lines for flattening");

            foreach (var orderLine in orderLines)
            {
                result.Add(FlattenJsonObject(orderLine));
            }

            Console.WriteLine($"Flattened {result.Count} order lines");
            return result;
        }

        static Dictionary<string, string> FlattenJsonObject(JToken token, string prefix = "")
        {
            var result = new Dictionary<string, string>();

            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in token.Children<JProperty>())
                    {
                        // Skip unwanted properties
                        if (property.Name.Equals("customLabels", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("deliveryScheduleIncludingRequests", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("documents", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("itemDetails", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("mergedItemDetails", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("predictions", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("pricesIncludingRequests", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("properties", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("proposal", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("reopenRequest", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("totalAmountIncludingRequests", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var childPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        foreach (var item in FlattenJsonObject(property.Value, childPrefix))
                        {
                            result[item.Key] = item.Value;
                        }
                    }
                    break;

                case JTokenType.Array:
                    var array = (JArray)token;
                    for (int i = 0; i < array.Count; i++)
                    {
                        var childPrefix = $"{prefix}[{i}]";
                        foreach (var item in FlattenJsonObject(array[i], childPrefix))
                        {
                            result[item.Key] = item.Value;
                        }
                    }
                    break;

                default:
                    result[prefix] = token.ToString();
                    break;
            }

            return result;
        }

        static void WriteToCsvFile(List<Dictionary<string, string>> orderLines)
        {
            Console.WriteLine($"WriteToCsvFile called with {orderLines.Count} order lines");

            if (!orderLines.Any())
            {
                Console.WriteLine("No order lines to export.");
                return;
            }

            // Get all unique column headers
            var allKeys = orderLines
                .SelectMany(dict => dict.Keys)
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            Console.WriteLine($"Found {allKeys.Count} unique column headers");
            Console.WriteLine($"Sample headers: {string.Join(", ", allKeys.Take(10))}");

            // Create CSV file with headers
            try
            {
                using (var writer = new StreamWriter(outputCsvFile))
                {
                    // Write header line
                    writer.WriteLine(string.Join(",", allKeys.Select(EscapeCsvField)));
                    Console.WriteLine("Header line written to CSV");

                    // Write data lines
                    foreach (var orderLine in orderLines)
                    {
                        var line = string.Join(",", allKeys.Select(key =>
                            orderLine.TryGetValue(key, out var value) ? EscapeCsvField(value) : ""));
                        writer.WriteLine(line);
                    }
                    Console.WriteLine($"All {orderLines.Count} data lines written to CSV");
                }

                Console.WriteLine($"Successfully exported {orderLines.Count} order lines to {outputCsvFile}");
                Console.WriteLine($"CSV file location: {Path.GetFullPath(outputCsvFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing CSV file: {ex.Message}");
            }
        }

        static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            bool needsQuotes = field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n");

            if (needsQuotes)
            {
                // Replace any double quotes with two double quotes
                field = field.Replace("\"", "\"\"");
                // Wrap the field in quotes
                return $"\"{field}\"";
            }

            return field;
        }
    }
}
