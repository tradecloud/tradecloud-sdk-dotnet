using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderBatch
    {   
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.test.tradecloud1.com/v2/api-connector/order";
    
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.test.tradecloud1.com/v2/api-connector/order/indicators";

        // Company IDs for testing
        static readonly List<string> companyIds = new List<string>
        {
            "f56aa4ce-8ec8-5197-bc26-77716a58add7",   // Agrifac
            "09484ff6-e0f0-510b-819f-5fa3ed780726",   // Voortman
            "ddd9cea1-510d-583c-8367-464172f98034"    // Rubitech
        };

        // Helper method for logging with timestamp
        static string LogTimestamp() => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]";

        static async Task Main(string[] args)
        {
            Console.WriteLine($"{LogTimestamp()} Tradecloud send order batch.");
            var orderJsonContent = File.ReadAllText("minimal-order.json");
            var orderIndicatorsJsonContent = File.ReadAllText("order-line-indicators-cancelled.json");

            var random = new Random();
            // Number of orders per company
            int ordersPerCompany = 1000;
            // Generate unique PO numbers for each company
            var purchaseOrderNumbersByCompany = companyIds.ToDictionary(
                companyId => companyId,
                companyId => Enumerable.Range(1, ordersPerCompany)
                    .Select(r => random.Next(1000, 1000000000).ToString("0000000000"))
                    .ToList()
            );

            Console.WriteLine($"{LogTimestamp()} Testing {companyIds.Count} companies in parallel with {ordersPerCompany} orders each");
            foreach (var companyId in companyIds)
            {
                var poNumbers = purchaseOrderNumbersByCompany[companyId];
                Console.WriteLine($"{LogTimestamp()} Company {companyId}: {poNumbers.Count()} orders, " + 
                    $"first 3: {string.Join(", ", poNumbers.Take(3))}, " + 
                    $"last 3: {string.Join(", ", poNumbers.TakeLast(3))}");
            }

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            try {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                
                // Process each company in parallel
                await Task.WhenAll(companyIds.Select(companyId => 
                    ProcessCompany(companyId, purchaseOrderNumbersByCompany[companyId])
                ));
                
                watch.Stop();
                Console.WriteLine($"{LogTimestamp()} All companies processed, took: {watch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex) {
                httpClient.CancelPendingRequests();
                Console.WriteLine($"{LogTimestamp()} Exception: {ex}");
            }

            async Task ProcessCompany(string companyId, List<string> poNumbers)
            {
                Console.WriteLine($"{LogTimestamp()} Starting processing for company {companyId}");
                var companyWatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Send orders for this company (serially within company)
                foreach (var poNumber in poNumbers)
                {
                    await SendOrder(companyId, poNumber);
                }
                
                // Await order service processing before cancelling as the API connector checks if the order exists
                var awaitTime = 1000 + poNumbers.Count * 100;         
                Console.WriteLine($"{LogTimestamp()} Awaiting order service processing for company {companyId} before cancelling... {awaitTime} ms");
                await Task.Delay(awaitTime);
                
                // Send cancellations
                foreach (var poNumber in poNumbers)
                {
                    await SendOrderIndicators(companyId, poNumber);
                }
                
                companyWatch.Stop();
                Console.WriteLine($"{LogTimestamp()} Company {companyId} processing complete, took: {companyWatch.ElapsedMilliseconds} ms");
            }

           async Task SendOrder(string companyId, string purchaseOrderNumber)
           {
                var jsonContent = orderJsonContent
                    .Replace("{purchaseOrderNumber}", purchaseOrderNumber)
                    .Replace("{companyId}", companyId);
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(sendOrderUrl, content);

                var statusCode = (int)response.StatusCode;
                if (statusCode == 400) {
                     Console.WriteLine($"{LogTimestamp()} SendOrder company={companyId} status={statusCode} request body={jsonContent}"); 
                }                
                if (statusCode != 200) {
                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"{LogTimestamp()} SendOrder company={companyId} status={statusCode} response body={responseString}");
                }
           }

           async Task SendOrderIndicators(string companyId, string purchaseOrderNumber)
           {
                var jsonContent = orderIndicatorsJsonContent
                    .Replace("{purchaseOrderNumber}", purchaseOrderNumber)
                    .Replace("{companyId}", companyId);
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);

                var statusCode = (int)response.StatusCode;
                if (statusCode == 400) {
                     Console.WriteLine($"{LogTimestamp()} SendOrderIndicators company={companyId} status={statusCode} request body={jsonContent}"); 
                }                
                if (statusCode != 200) {
                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"{LogTimestamp()} SendOrderIndicators company={companyId} status={statusCode} response body={responseString}");
                }
           }
        }
    }
}
