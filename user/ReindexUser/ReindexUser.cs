using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class ReindexUser
    {
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user/private/specs.yaml#/user/reindexForEntityIds
        const string reindexUserUrl = "https://api.tradecloud1.com/v2/user/reindex";

        // Check/amend mandatory user id
        const string jsonContentWithSingleQuotes =
            @"{
              `entityIds`: [
                `c97cf226-09ed-44d0-90de-bd5b28d76568`
              ]
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud reindex user example.");

            HttpClient httpClient = new HttpClient();
            var accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJkYXRhIjp7InVzZXJuYW1lIjoibWFyY2VsQHRyYWRlY2xvdWQxLmNvbSIsInVzZXJJZCI6ImYxY2YzNDA0LTMxMTktNDllYi05NGNlLTkxYWU0ZTY1NTc5ZCIsInVzZXJSb2xlcyI6WyJzdXBwb3J0Il0sImNvbXBhbnlSb2xlcyI6W10sImF1dGhvcml6ZWRDb21wYW55SWRzIjpbXSwiY29tcGFueUlkIjoiMDY4OTNiYmEtZTEzMS00MjY4LTg3YzktN2ZhZTY0ZTE2ZWU5IiwidHdvRkFFbmFibGVkIjp0cnVlLCJ0d29GQUVuZm9yY2VkIjp0cnVlLCJzdGF0dXMiOiJhdXRoZW50aWNhdGVkIiwiaWRlbnRpdHlQcm92aWRlciI6InRyYWRlY2xvdWQifSwiZXhwIjoxNzMyODA2MDIxfQ.iNTLNXvjiGIO5VjZc26JhhNEAzUdZEZDAW0LFjcFeFo";
            await ReindexUser(accessToken);

            async Task ReindexUser(string accessToken)
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(reindexUserUrl, content);
                watch.Stop();
                Console.WriteLine("ReindexUser StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("ReindexUser Body: " + responseString);
            }
        }
    }
}
