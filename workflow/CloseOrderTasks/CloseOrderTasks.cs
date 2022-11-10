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
    class CloseOrderTasks
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/closeOrderTasks
        const string sendOrderUrl = "https://api.accp.tradecloud1.com/v2/workflow/order/close";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Close orders batch.");

            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            if (!String.IsNullOrEmpty(accessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

                using(var log = new StreamWriter("CloseOrderTasks.log", append: true) )
                {
                    using(var reader = new StreamReader("filename.csv"))
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

                async Task TestCloseOrderTasks(OrderTasks orderTasks, StreamWriter log) 
                {
                    string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                    Console.WriteLine("CloseOrderTasks " + json);
                    await log.WriteLineAsync("CloseOrderTasks " + json);
                }

                async Task CloseOrderTasks(OrderTasks orderTasks, StreamWriter log)
                {                
                    string json = JsonConvert.SerializeObject(orderTasks, Formatting.Indented);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    var summary = "CloseOrderTasks start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase + " json=" + json;
                    Console.WriteLine(summary);
                    await log.WriteLineAsync(summary);

                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200) {
                        Console.WriteLine("CloseOrderTasks response body=" +  responseString);
                        await log.WriteLineAsync("CloseOrderTasks response body=" +  responseString);
                    }         
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
