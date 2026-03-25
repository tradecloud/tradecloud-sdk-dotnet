using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using DotNetEnv;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // TODO paging is broken
    class ReassignBuyerContact
    {
        // Fill in the source buyer contact user id to search for
        const string sourceUserId = "";
        // Fill in the target buyer contact user id to reassign to
        const string targetUserId = "";

        // Set to false to actually reassign contacts
        static bool dryRun = true;

        // When true, search only orders with process status Issued, InProgress, or Confirmed.
        // When false, search all orders with this buyer contact (matches counts that include Completed/Cancelled/Rejected).
        static bool restrictSearchToActiveProcessStatuses = true;

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/reassignBuyerContact
        const string reassignBuyerContactUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/buyer/contact/reassign";

        const string queryTemplateContactOnlyWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'contact': {
                        'userIds': ['{sourceUserId}']
                    }
                }
            },
            'offset': {offset},
            'limit': {limit}
        }";

        const string queryTemplateContactAndActiveStatusWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'contact': {
                        'userIds': ['{sourceUserId}']
                    }
                },
                'status': {
                    'processStatus': ['Issued', 'InProgress', 'Confirmed']
                }
            },
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;

        const string reassignBodyTemplateWithSingleQuotes = @"{
            'userId': '{targetUserId}',
            'version': {version}
        }";

        static async Task Main(string[] args)
        {
            LoadEnvFile();
            var accessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Console.WriteLine("Error: ACCESS_TOKEN is not set. Copy .env.example to .env in this project folder and set ACCESS_TOKEN to your API bearer token.");
                return;
            }

            Console.WriteLine("Tradecloud reassign buyer contact." + (dryRun ? " (DRY RUN)" : "") + (restrictSearchToActiveProcessStatuses ? " [active process statuses only]" : " [all process statuses]"));

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            int offset = 0;
            int total = limit;
            int reassigned = 0;
            while (total > offset)
            {
                var queryResult = await SearchOrders(offset);
                if (queryResult == null)
                {
                    break;
                }

                total = ((int)queryResult["total"]);
                var ordersArray = queryResult.Properties()
                    .Select(p => p.Value)
                    .OfType<JArray>()
                    .FirstOrDefault();
                if (ordersArray == null)
                {
                    Console.WriteLine("SearchOrders: no order array in response; expected a property with a JSON array value.");
                    break;
                }

                Console.WriteLine("total=" + total + " offset=" + offset + " pageSize=" + ordersArray.Count);

                if (ordersArray.Count == 0)
                {
                    break;
                }

                foreach (JObject order in ordersArray)
                {
                    string orderId = order["id"].ToString();
                    string purchaseOrderNumber = order["buyerOrder"]["purchaseOrderNumber"].ToString();
                    long version = order["version"].Value<long>();

                    if (dryRun)
                    {
                        Console.WriteLine("DRY RUN: would reassign orderId=" + orderId + " purchaseOrderNumber=" + purchaseOrderNumber + " version=" + version);
                    }
                    else
                    {
                        await ReassignContact(orderId, purchaseOrderNumber, version);
                    }
                    reassigned++;
                }

                offset += ordersArray.Count;
            }

            Console.WriteLine("Done. " + (dryRun ? "Would have reassigned " : "Reassigned ") + reassigned + " orders.");

            async Task<JObject> SearchOrders(int offset)
            {
                var queryTemplate = (restrictSearchToActiveProcessStatuses
                        ? queryTemplateContactAndActiveStatusWithSingleQuotes
                        : queryTemplateContactOnlyWithSingleQuotes)
                    .Replace("'", "\"");
                var query = queryTemplate
                    .Replace("{sourceUserId}", sourceUserId)
                    .Replace("{offset}", offset.ToString())
                    .Replace("{limit}", limit.ToString());
                var content = new StringContent(query, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(orderSearchUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchOrders start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
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

            async Task ReassignContact(string orderId, string purchaseOrderNumber, long version)
            {
                var reassignUrl = HttpUtility.UrlPathEncode(reassignBuyerContactUrlTemplate.Replace("{orderId}", orderId));
                var bodyTemplate = reassignBodyTemplateWithSingleQuotes.Replace("'", "\"");
                var body = bodyTemplate
                    .Replace("{targetUserId}", targetUserId)
                    .Replace("{version}", version.ToString());
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(reassignUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ReassignBuyerContact orderId=" + orderId + " purchaseOrderNumber=" + purchaseOrderNumber + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode != 200)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("ReassignBuyerContact response body=" + responseString);
                }
            }
        }

        /// <summary>Loads .env from the project directory (next to the .csproj) when running via dotnet run, or from the current directory.</summary>
        static void LoadEnvFile()
        {
            var nextToProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
            if (File.Exists(nextToProject))
            {
                Env.Load(nextToProject);
                return;
            }

            var cwdEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(cwdEnv))
            {
                Env.Load(cwdEnv);
            }
        }
    }
}
