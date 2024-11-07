using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class ExportOrderLines
    {
        const string username = "";
        const string password = "";
        const string env = "accp"; // one of `accp` or `portal` (for production)
        const string dateFrom = "2024-01-01";
        const string dateTo = "2025-01-01";
        const string fileName = "order-lines-export.csv";

        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderLinesExport        
        const string urlTemplate = "https://{env}.tradecloud.nl/api/v1/purchaseOrderLine/_export?dateFrom={dateFrom}&dateTo={dateTo}&header={header}&page={page}&sort=codeAndRow&sortOrder=asc";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud export order lines example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);

            int page = 1;
            bool fetchHeader = true;
            using (var csvWriter = new StreamWriter(fileName, append: false))
            {
                while (true)
                {
                    var result = await ExportOrderLines(page, fetchHeader, csvWriter);
                    if (result == 404)
                    {
                        Console.WriteLine($"Reached end of pages at page {page}");
                        break;
                    }
                    if (result != 200)
                    {
                        Console.WriteLine($"Error: Received status code {result} on page {page}");
                        break;
                    }
                    fetchHeader = false; // Only fetch header for the first page
                    page++;
                }
            }

            async Task<int> ExportOrderLines(int page, bool fetchHeader, StreamWriter csvWriter)
            {
                var headerValue = fetchHeader ? "true" : "false";
                var url = urlTemplate.Replace("{env}", env)
                                     .Replace("{dateFrom}", dateFrom)
                                     .Replace("{dateTo}", dateTo)
                                     .Replace("{header}", headerValue)
                                     .Replace("{page}", page.ToString());

                var response = await httpClient.GetAsync(url);
                var statusCode = (int)response.StatusCode;
                if (statusCode == 404)
                {
                    return 404;
                }

                string responseString = await response.Content.ReadAsStringAsync();
                await csvWriter.WriteLineAsync(responseString);
                return statusCode;
            }
        }
    }
}
