using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SetDeliverySchedule
    {   
        const string orderId = "";
        const string orderLinePosition = "";
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/updateOrderLineDeliverySchedule
        const string setDeliveryScheduleUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{id}/line/{position}/deliverySchedule";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud set delivery schedule as supplier example.");
            
            var jsonContent = File.ReadAllText(@"delivery-schedule.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            

            await SetDeliverySchedule();

            async Task SetDeliverySchedule()
            {                
                var setDeliveryScheduleUrl = setDeliveryScheduleUrlTemplate.Replace("{id}", orderId).Replace("{position}", orderLinePosition); 
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(setDeliveryScheduleUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SetDeliverySchedule start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SetDeliverySchedule request body=" + jsonContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("SetDeliverySchedule response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("SetDeliverySchedule response body=" +  responseString);
            }
        }
    }
}
