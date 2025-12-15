using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchTasks
    {
        const string accessToken = "";
        const string assigneeCompanyId = "{companyId}"; // Tasks assigned to this buyer or supplier.
        const string assigneeContactId = "{contactId}"; // Tasks assigned to this user of above buyer or supplier.

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/workflow/private/specs.yaml#/workflow/searchWorkflowTasksRoute
        const string workflowSearchUrl = "https://api.accp.tradecloud1.com/v2/workflow/search";

        const string queryTemplateFile = "query-template.json";
        const int limit = 100;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Search workflow tasks.");

            string outputFile = "task_responses.json";

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            int offset = 0;
            int total = limit;
            int responseCount = 0;
            while (total > offset)
            {
                var queryResult = await SearchWorkflow(httpClient, offset);
                if (queryResult != null)
                {
                    total = ((int)queryResult["total"]);
                    Console.WriteLine("total=" + total + " offset=" + offset);
                    offset += limit;

                    foreach (var task in queryResult.First.Values())
                    {
                        responseCount++;
                        File.AppendAllText(outputFile, task.ToString() + Environment.NewLine);
                    }
                }
                else
                {
                    total = 0;
                }
            }

            Console.WriteLine($"Saved {responseCount} responses to {outputFile}");
        }

        static async Task<JObject> SearchWorkflow(HttpClient httpClient, int offset)
        {
            string queryTemplate = File.ReadAllText(queryTemplateFile);
            var query = queryTemplate.Replace("{companyId}", assigneeCompanyId).Replace("{contactId}", assigneeContactId).Replace("{offset}", offset.ToString()).Replace("{limit}", limit.ToString());
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
    }
}

public class OrderTasks
{
    public string orderId { get; set; }
    public IList<string> taskIds { get; set; }
}
