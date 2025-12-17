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
    class ApproveShipmentRequest
    {
        const string accessToken = "";
        const string orderId = "";
        const string linePosition = "";

        // Set to "approve" or "reject"
        const string action = "approve";

        // Use approve-request.json for approve, reject-request.json for reject
        const string approveBody = "approve-request.json";
        const string rejectBody = "reject-request.json";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/approveShipmentRescheduleRequestRoute
        const string approveUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/line/{position}/reschedule/approve";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/rejectShipmentRescheduleRequestRoute
        const string rejectUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/line/{position}/reschedule/reject";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud approve or reject shipment reschedule request example.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            if (action == "approve")
            {
                await ApproveRescheduleRequest(httpClient);
            }
            else if (action == "reject")
            {
                await RejectRescheduleRequest(httpClient);
            }
            else
            {
                Console.WriteLine("Invalid action. Use 'approve' or 'reject'.");
            }
        }

        static async Task ApproveRescheduleRequest(HttpClient httpClient)
        {
            var jsonContent = File.ReadAllText(@approveBody);
            var approveUrl = HttpUtility.UrlPathEncode(
                approveUrlTemplate
                    .Replace("{orderId}", orderId)
                    .Replace("{position}", linePosition)
            );
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(approveUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("ApproveRescheduleRequest start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            if (statusCode == 400)
                Console.WriteLine("ApproveRescheduleRequest request body=" + jsonContent);
            string responseString = await response.Content.ReadAsStringAsync();
            if (statusCode == 200)
                Console.WriteLine("ApproveRescheduleRequest response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
            else
                Console.WriteLine("ApproveRescheduleRequest response body=" + responseString);
        }

        static async Task RejectRescheduleRequest(HttpClient httpClient)
        {
            var jsonContent = File.ReadAllText(@rejectBody);
            var rejectUrl = HttpUtility.UrlPathEncode(
                rejectUrlTemplate
                    .Replace("{orderId}", orderId)
                    .Replace("{position}", linePosition)
            );
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(rejectUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("RejectRescheduleRequest start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            if (statusCode == 400)
                Console.WriteLine("RejectRescheduleRequest request body=" + jsonContent);
            string responseString = await response.Content.ReadAsStringAsync();
            if (statusCode == 200)
                Console.WriteLine("RejectRescheduleRequest response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
            else
                Console.WriteLine("RejectRescheduleRequest response body=" + responseString);
        }
    }
}

