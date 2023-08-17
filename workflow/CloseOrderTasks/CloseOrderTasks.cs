using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Threading;

namespace Com.Tradecloud1.SDK.Client
{
    class CloseOrderTasks
    {   
        const bool dryRun = true;
        static string accessToken = ""; // required when not setting a refresh token
        static string refreshToken = ""; // required when the script is expected to take > 10 mins.

         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.tradecloud1.com/v2/authentication/";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/closeOrderTasks
        const string closeOrderTasksUrl = "https://api.tradecloud1.com/v2/workflow/order/close";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Close orders batch.");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using(var log = new StreamWriter("close-order-tasks.log", append: true) )
            {
                using(var reader = new StreamReader("close-order-tasks.csv"))
                {
                    string lastOrderId = null;
                    OrderTasks orderTasks = null;
                    var success = true;
                    while (!reader.EndOfStream && success)
                    {   
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        var currentOrderId = values[1];
                        var currentTaskId =  values[0];

                        if (currentOrderId != lastOrderId) {
                            if (orderTasks != null)
                            {
                                success = await CloseOrderTasks(orderTasks, log); 
                            }

                            orderTasks = new OrderTasks
                            {
                                orderId = currentOrderId,
                                taskIds = new List<string> { currentTaskId } 
                            };

                            lastOrderId = currentOrderId;
                        }
                        else
                        {
                            orderTasks.taskIds.Add(currentTaskId);
                        }                        
                    }
                    await CloseOrderTasks(orderTasks, log);
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
                    
                    if (statusCode == 401 && refreshToken != "")
                    {                            
                        // Refresh access and refresh tokens
                        (accessToken, refreshToken) = await authenticationClient.Refresh(refreshToken);
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        // Retry once with refreshed access token
                        statusCode = await RealRunCloseOrderTasks(orderTasks, log);
                    }
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

            async Task<int>  DryRunCloseOrderTasks(OrderTasks orderTasks, StreamWriter log) 
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
                var summary = "RealRunCloseOrderTasks start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " json=" + json;
                Console.WriteLine(summary);
                await log.WriteLineAsync(summary);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200) {
                    Console.WriteLine("RealRunCloseOrderTasks response body=" +  responseString);
                    await log.WriteLineAsync("RealRunCloseOrderTasks response body=" +  responseString);
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
