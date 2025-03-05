using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class PublishActiveTasks
    {
        // Fill in mandatory super user token
        const string token = "";

        // Fill in mandatory companyId
        const string companyId = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/publishActiveOrder
        const string publishActiveTasksUrl = "https://api.accp.tradecloud1.com/v2/workflow/publishActiveTasks";


        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud publish active tasks.");

            var jsonContentTemplate = File.ReadAllText(@"publish-active-tasks.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await PublishActiveTasks();

            async Task PublishActiveTasks()
            {
                var jsonContent = jsonContentTemplate.Replace("{newDate}", DateTime.Now.ToString("yyyy-MM-dd"));

                jsonContent = jsonContent.Replace("{companyId}", companyId);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(publishActiveTasksUrl, content);
                watch.Stop();


                var statusCode = (int)response.StatusCode;
                Console.WriteLine("PublishActiveTasks start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("PublishActiveTasks request body=" + jsonContent);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("PublishActiveTasks response body=" + JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("PublishActiveTasks response body=" + responseString);
            }
        }
    }
}
