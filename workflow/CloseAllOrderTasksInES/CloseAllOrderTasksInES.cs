using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class CloseOrderTasksInES
    {   
        const string accessToken = "";
        const string companyId = "{companyId}"; // Close tasks for this buyer or supplier.  
        const string orderIdToClose = "{buyerId}-{purchaseOrderNumber}"; // Close all tasks related to this `orderId` for above buyer or supplier.

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/searchWorkflowTasksRoute
        const string workflowSearchUrl = "https://api.accp.tradecloud1.com/v2/workflow/search";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'assignedTo': 'MyCompany',
                'companyId': '{companyId}',
                'taskStatus': 'created' 
            },
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 10;

        // This API resource will also clean up tasks in Elasticsearch when not available in Cassandra anymore. 
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/closeOrderTasks
        const string closeOrderTasksUrl = "https://api.accp.tradecloud1.com/v2/workflow/order/close";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Close order workflow tasks in ES.");

            var orderTasksToClose = new OrderTasks
            {
                orderId = orderIdToClose,
                taskIds = new List<string>()
            };

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

            //string lastOrderId = null;
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
                        string orderId = null;
                        if (task["content"] != null && task["content"]["order"] != null && task["content"]["order"]["orderId"] != null)
                        {
                            orderId = task["content"]["order"]["orderId"].ToString();
                            Console.WriteLine("CloseOrderTasksInES found content/order/orderId=" + orderId);
                        }
                        else if (task["content"] != null && task["content"]["orderLine"] != null && task["content"]["orderLine"]["orderId"] != null)
                        {
                            orderId = task["content"]["orderLine"]["orderId"].ToString();
                            Console.WriteLine("CloseOrderTasksInES found content/orderLine/orderId=" + orderId);
                        }

                        if (orderId == orderIdToClose && task["id"] != null)
                        {
                           string taskId = task["id"].ToString();
                           Console.WriteLine("CloseOrderTasksInES found orderId=" + orderId + " taskId=" + taskId);
                           orderTasksToClose.taskIds.Add(taskId);                                 
                        }
                    }
                }
                else {
                    total = 0;
                }
            }
            
            //TestCloseOrderTasks(orderTasksToClose);
            await CloseOrderTasks(orderTasksToClose);

            async Task<JObject> SearchWorkflow(int offset)
            {
                var queryTemplate = queryTemplateWithSingleQuotes.Replace("'", "\"");
                var query = queryTemplate.Replace("{companyId}", companyId).Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
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

            void TestCloseOrderTasks(OrderTasks orderTasks) 
            {
                string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                Console.WriteLine("CloseOrderTasksInES " + json);
            }

            async Task CloseOrderTasks(OrderTasks orderTasks)
            {                
                string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(closeOrderTasksUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("CloseOrderTasksInES start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " json=" + json);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200) {
                    Console.WriteLine("CloseOrderTasksInES response body=" +  responseString);
                }         
            }
        }
    }

    public class OrderTasks
    {
        public string orderId { get; set; }
        public IList<string> taskIds { get; set; }
    }
}
