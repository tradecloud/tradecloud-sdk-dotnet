using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Com.Tradecloud1.SDK.Client
{
    class CloseOrderTasks
    {   
        const bool dryRun = true;
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/closeOrderTasks
        const string closeOrderTasksUrl = "https://api.accp.tradecloud1.com/v2/workflow/order/close";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Close orders batch.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

            using(var log = new StreamWriter("close-order-tasks.log", append: true) )
            {
                using(var reader = new StreamReader("close-order-tasks.csv"))
                {
                    string lastOrderId = null;
                    OrderTasks orderTasks = null;
                    while (!reader.EndOfStream)
                    {   
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        var currentOrderId = values[1];
                        var currentTaskId =  values[0];

                        if (currentOrderId != lastOrderId) {
                            if (orderTasks != null)
                            {
                                await CloseOrderTasks(orderTasks, log); 
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

            async Task CloseOrderTasks(OrderTasks orderTasks, StreamWriter log) 
            {  
                if (dryRun) 
                {
                    await DryRunCloseOrderTasks(orderTasks, log);
                }
                else
                {
                    await RealRunCloseOrderTasks(orderTasks, log);
                }
            }

            async Task DryRunCloseOrderTasks(OrderTasks orderTasks, StreamWriter log) 
            {
                string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                Console.WriteLine("DryRunCloseOrderTasks " + json);
                await log.WriteLineAsync("DryRunCloseOrderTasks " + json);
            }

            async Task RealRunCloseOrderTasks(OrderTasks orderTasks, StreamWriter log)
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
            }
        }
    }

    public class OrderTasks
    {
        public string orderId { get; set; }
        public IList<string> taskIds { get; set; }
    }
}
