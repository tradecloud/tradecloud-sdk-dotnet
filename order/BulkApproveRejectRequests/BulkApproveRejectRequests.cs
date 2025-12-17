using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class BulkApproveRejectRequests
    {
        const string accessToken = "";
        const string orderId = "";

        // Set to "approve" or "reject"
        const string action = "approve";

        // Use bulk-approve-request.json for approve, bulk-reject-request.json for reject
        const string approveBody = "bulk-approve-request.json";
        const string rejectBody = "bulk-reject-request.json";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/approveRequestsAsBuyerRoute
        const string approveUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/requests/buyer/approve";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/rejectRequestsAsBuyerRoute
        const string rejectUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/requests/buyer/reject";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud bulk approve or reject requests as buyer example.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            if (action == "approve")
            {
                await BulkApproveRequests(httpClient);
            }
            else if (action == "reject")
            {
                await BulkRejectRequests(httpClient);
            }
            else
            {
                Console.WriteLine("Invalid action. Use 'approve' or 'reject'.");
            }
        }

        static async Task BulkApproveRequests(HttpClient httpClient)
        {
            var jsonContent = File.ReadAllText(@approveBody);
            var approveUrl = HttpUtility.UrlPathEncode(
                approveUrlTemplate.Replace("{orderId}", orderId)
            );
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(approveUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("BulkApproveRequests start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            if (statusCode == 400)
                Console.WriteLine("BulkApproveRequests request body=" + jsonContent);
            string responseString = await response.Content.ReadAsStringAsync();
            if (statusCode == 200)
                Console.WriteLine("BulkApproveRequests response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
            else
                Console.WriteLine("BulkApproveRequests response body=" + responseString);
        }

        static async Task BulkRejectRequests(HttpClient httpClient)
        {
            var jsonContent = File.ReadAllText(@rejectBody);
            var rejectUrl = HttpUtility.UrlPathEncode(
                rejectUrlTemplate.Replace("{orderId}", orderId)
            );
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(rejectUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("BulkRejectRequests start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            if (statusCode == 400)
                Console.WriteLine("BulkRejectRequests request body=" + jsonContent);
            string responseString = await response.Content.ReadAsStringAsync();
            if (statusCode == 200)
                Console.WriteLine("BulkRejectRequests response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
            else
                Console.WriteLine("BulkRejectRequests response body=" + responseString);
        }
    }
}

