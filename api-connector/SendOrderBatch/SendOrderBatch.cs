using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderBatch
    {   
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.test.tradecloud1.com/v2/api-connector/order";
    
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.test.tradecloud1.com/v2/api-connector/order/indicators";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order batch.");
            var orderJsonContent = File.ReadAllText("minimal-order.json");
            var orderIndicatorsJsonContent = File.ReadAllText("order-line-indicators-cancelled.json");

            var random = new Random();
             // 500 is the max. parallism for a single API connector instance in a test environment
            var purchaseOrderNumbers = Enumerable.Range(1,10000).Select(r => random.Next(1000, 1000000000).ToString("0000000000")).ToList();
            Console.WriteLine("Issueing and canceling batch orders, count: " + purchaseOrderNumbers.Count() + 
                ", first 5: " + string.Join(", ", purchaseOrderNumbers.Take(5)) + 
                ", last 5: " + string.Join(", ", purchaseOrderNumbers.TakeLast(5)));

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            try {                
                var watch = System.Diagnostics.Stopwatch.StartNew();
                // await Task.WhenAll(purchaseOrderNumbers.Select(SendOrder)); - parallel execution
                foreach (var poNumber in purchaseOrderNumbers)
                {
                    await SendOrder(poNumber); // waits for each one before starting the next
                }
                watch.Stop();
                Console.WriteLine("Serial sending orders done, took: " + watch.ElapsedMilliseconds + " ms");

                // Await order service processing before cancelling as the API connector checks if the order exists
                var awaitTime = purchaseOrderNumbers.Count() * 10;         
                Console.WriteLine("Awaiting order service processing before cancelling... " + awaitTime + " ms");
                Thread.Sleep(awaitTime);        

                watch = System.Diagnostics.Stopwatch.StartNew();
                // await Task.WhenAll(purchaseOrderNumbers.Select(SendOrderIndicators)); - parallel execution
                foreach (var poNumber in purchaseOrderNumbers)
                {
                    await SendOrderIndicators(poNumber); // waits for each one before starting the next
                }
                watch.Stop();
                Console.WriteLine("Serial sending order indicators, took: " + watch.ElapsedMilliseconds + " ms");
            }
            catch (Exception ex) {
                httpClient.CancelPendingRequests();
                Console.WriteLine(ex);
            }

           async Task SendOrder(string purchaseOrderNumber)
           {
                var jsonContent = orderJsonContent.Replace("{purchaseOrderNumber}", purchaseOrderNumber);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                //Console.WriteLine("Sending order: " + jsonContent);
                var response = await httpClient.PostAsync(sendOrderUrl, content);

                var statusCode = (int)response.StatusCode;
                if (statusCode == 400) {
                     Console.WriteLine("SendOrder status=" + statusCode + " request body=" + jsonContent); 
                }                
                if (statusCode != 200) {
                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("SendOrder status=" + statusCode + " response body=" +  responseString);
                }
           }

           async Task SendOrderIndicators(string purchaseOrderNumber)
           {
                var jsonContent = orderIndicatorsJsonContent.Replace("{purchaseOrderNumber}", purchaseOrderNumber);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                //Console.WriteLine("Sending order indicators: " + jsonContent);
                var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);

                var statusCode = (int)response.StatusCode;
                if (statusCode == 400) {
                     Console.WriteLine("SendOrderIndicators status=" + statusCode + " request body=" + jsonContent); 
                }                
                if (statusCode != 200) {
                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("SendOrderIndicators status=" + statusCode + " response body=" +  responseString);
                }
           }
        }
    }
}
