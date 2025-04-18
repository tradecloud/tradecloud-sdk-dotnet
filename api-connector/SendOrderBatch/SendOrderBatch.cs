using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderBatch
    {   
        const bool dryRun = true;
        const string accessToken = "";
        const int ordersPerCompany = 10;

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.test.tradecloud1.com/v2/api-connector/order";
    
        // Company IDs for testing
        static readonly List<string> companyIds = new List<string>
        {
            "f56aa4ce-8ec8-5197-bc26-77716a58add7",   // Agrifac
            "09484ff6-e0f0-510b-819f-5fa3ed780726",   // Voortman
            "ddd9cea1-510d-583c-8367-464172f98034"    // Rubitech
        };
        
        // Semaphore to control CSV file access
        static SemaphoreSlim csvSemaphore = new SemaphoreSlim(1, 1);
        
        // Semaphore to control log file access
        static SemaphoreSlim logSemaphore = new SemaphoreSlim(1, 1);

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order batch.");

            var orderJsonContentTemplate = File.ReadAllText("order.json");            

            // Generate unique PO numbers for each company
            var random = new Random();
            var purchaseOrderNumbersByCompany = companyIds.ToDictionary(
                companyId => companyId,
                companyId => Enumerable.Range(1, ordersPerCompany)
                    .Select(r => random.Next(1000, 1000000000).ToString("0000000000"))
                    .ToList()
            );

            // Initialize the CSV file with headers
            using (var writer = new StreamWriter("orders.csv", false))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<Order>();
                csv.NextRecord();
            }
            
            using (var log = new StreamWriter("send_order_batch.log", append: true))
            {
                Console.WriteLine($"Testing {companyIds.Count} companies in parallel with {ordersPerCompany} orders each");
                foreach (var companyId in companyIds)
                {
                    var poNumbers = purchaseOrderNumbersByCompany[companyId];
                    await WriteToLogAsync(log, $"Company {companyId}: {poNumbers.Count()} orders, " + 
                        $"first 3: {string.Join(", ", poNumbers.Take(3))}, " + 
                        $"last 3: {string.Join(", ", poNumbers.TakeLast(3))}");
                }

                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var watch = System.Diagnostics.Stopwatch.StartNew();
                
                // Process each company in parallel
                await Task.WhenAll(companyIds.Select(companyId => 
                    ProcessCompany(companyId, purchaseOrderNumbersByCompany[companyId], log, httpClient)
                ));
                
                watch.Stop();
                await WriteToLogAsync(log, $"All companies processed, took: {watch.ElapsedMilliseconds} ms");
            }

            async Task ProcessCompany(
                string companyId, 
                List<string> poNumbers, 
                StreamWriter log, 
                HttpClient httpClient)
            {
                var companyWatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Send orders for this company (serially within company)
                foreach (var poNumber in poNumbers)
                {
                    bool success = await SendOrder(companyId, poNumber, log, httpClient);
                    
                    // Add to CSV file if the order was sent successfully
                    if (success)
                    {
                        await AppendOrderToCsv(new Order { companyId = companyId, purchaseOrderNumber = poNumber });
                    }
                }
                
                companyWatch.Stop();
                await WriteToLogAsync(log, $"Company {companyId} processing complete, took: {companyWatch.ElapsedMilliseconds} ms");
            }

            async Task<bool> SendOrder(string companyId, string purchaseOrderNumber, StreamWriter log, HttpClient httpClient)
            {
                var jsonContent = orderJsonContentTemplate
                    .Replace("{companyId}", companyId)
                    .Replace("{purchaseOrderNumber}", purchaseOrderNumber);

                if (dryRun) 
                {
                    await WriteToLogAsync(log, $"SendOrder dry run companyId={companyId} purchaseOrderNumber={purchaseOrderNumber} content={jsonContent}");
                    return true; // Assume success for dry run
                }        
                else
                {
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(sendOrderUrl, content);

                    var statusCode = (int)response.StatusCode;
                    if (statusCode == 400) 
                    {
                        await WriteToLogAsync(log, $"SendOrder company={companyId} status={statusCode} request body={jsonContent}"); 
                        return false;
                    }                
                    if (statusCode != 200) 
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        await WriteToLogAsync(log, $"SendOrder company={companyId} status={statusCode} response body={responseString}");
                        return false;
                    }
                    
                    return true; // Successfully sent
                }
            }
            
            async Task AppendOrderToCsv(Order order)
            {
                // Use semaphore to ensure only one thread writes to the file at a time
                await csvSemaphore.WaitAsync();
                try
                {
                    using (var writer = new StreamWriter("orders.csv", true))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecord(order);
                        csv.NextRecord();
                    }
                }
                finally
                {
                    csvSemaphore.Release();
                }
            }
            
            async Task WriteToLogAsync(StreamWriter log, string message)
            {
                // Use semaphore to ensure only one thread writes to the log at a time
                await logSemaphore.WaitAsync();
                try
                {
                    await log.WriteLineAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
                }
                finally
                {
                    logSemaphore.Release();
                }
            }
        }
    }

    public class Order
    {
        public string companyId { get; set; }
        public string purchaseOrderNumber { get; set; }
    }
}
