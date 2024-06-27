using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Com.Tradecloud1.SDK.Client
{
    class CloseOrderTasksSearchBatch
    {
        const bool dryRun = true;
        static string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/searchWorkflowTasksRoute
        const string workflowSearchUrl = "https://api.accp.tradecloud1.com/v2/workflow/search";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/closeOrderTasks
        const string closeOrderTasksUrl = "https://api.accp.tradecloud1.com/v2/workflow/order/close";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'taskStatus': 'created',
                'companyId': '{companydId}',
                'assignee': {
                    'assignedTo': 'MySuppliers',
                    'includeUnassigned': false,
                    'userIds': []
                },
                'classification': {
                    'modules': ['Purchase'],
                    'categories': ['Exceptions']
                }
            },
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;
        static async Task Main(string[] args)
        {
            Console.WriteLine("Close orders batch.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("close-order-tasks.log", append: true))
            {
                int offset = 0;
                int total = limit;
                while (total > offset)
                {
                    var queryResult = await SearchWorkflow(offset);
                    if (queryResult != null)
                    {
                        total = ((int)queryResult["total"]);
                        Console.WriteLine("total=" + total + " offset=" + offset);
                        offset += limit;

                        foreach (var task in queryResult.First.Values())
                        {
                            string taskId = task["id"].ToString();
                            // assumes order line tasks only
                            string orderId = task["content"]["orderLine"]["orderId"].ToString();
                            string eventName = task["eventName"].ToString();

                            // optional additional filtering
                            if (eventName == "OrderLinesCancelledByBuyer")
                            {
                                var orderTasks = new OrderTasks()
                                {
                                    orderId = orderId,
                                    taskIds = new List<string> { taskId }
                                };
                                await CloseOrderTasks(orderTasks, log);
                            }
                        }
                    }
                    else
                    {
                        total = 0;
                    }
                }
            }

            async Task<JObject> SearchWorkflow(int offset)
            {
                var queryTemplate = queryTemplateWithSingleQuotes.Replace("'", "\"");
                var query = queryTemplate.Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
                var content = new StringContent(query, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(workflowSearchUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SearchWorkflow start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    Console.WriteLine("SearchWorkflow request body=" + query);
                    Console.WriteLine("SearchWorkflow response body=" + responseString);
                    return null;
                }
            }

            async Task<bool> CloseOrderTasks(OrderTasks orderTasks, StreamWriter log)
            {
                if (dryRun)
                {
                    await DryRunCloseOrderTasks(orderTasks, log);
                    return true;
                }
                else
                {
                    var statusCode = await RealRunCloseOrderTasks(orderTasks, log);

                    if (statusCode == 500)
                    {
                        // Expected time out when closing more than a few tasks
                        // Retry once to be sure, which should be quick
                        statusCode = await RealRunCloseOrderTasks(orderTasks, log);
                    }

                    // In case of not found the batch will continue
                    if (statusCode == 200 || statusCode == 404)
                        return true;
                    else
                        return false;
                }
            }

            async Task<int> DryRunCloseOrderTasks(OrderTasks orderTasks, StreamWriter log)
            {
                string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                Console.WriteLine("DryRunCloseOrderTasks " + json);
                await log.WriteLineAsync("DryRunCloseOrderTasks " + json);
                return 200;
            }

            async Task<int> RealRunCloseOrderTasks(OrderTasks orderTasks, StreamWriter log)
            {
                string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(closeOrderTasksUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                var summary = "RealRunCloseOrderTasks start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " json=" + json;
                Console.WriteLine(summary);
                await log.WriteLineAsync(summary);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200)
                {
                    Console.WriteLine("RealRunCloseOrderTasks response body=" + responseString);
                    await log.WriteLineAsync("RealRunCloseOrderTasks response body=" + responseString);
                }
                return statusCode;
            }
        }
    }

    public class OrderTasks
    {
        public string orderId { get; set; }
        public IList<string> taskIds { get; set; }
    }
}
