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
    class CloseAllOrderTasks
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/closeAllOrderTasks
        const string closeAllOrderTasksUrl = "https://api.accp.tradecloud1.com/v2/workflow/order/close/all";
        
        const string jsonContentWithSingleQuotes = @"{    
            'orderId': ''
        }";

        const string accessToken = "";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Close all order tasks.");
            
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            
            await CloseAllOrderTasks();
              
            async Task CloseAllOrderTasks()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(closeAllOrderTasksUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("CloseAllOrderTasks start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);                

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200) {
                    Console.WriteLine("CloseAllOrderTasks response body=" +  responseString);
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
