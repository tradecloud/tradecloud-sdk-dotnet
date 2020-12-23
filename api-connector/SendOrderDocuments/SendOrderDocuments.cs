using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/attachOrderDocumentsByBuyerRoute
        const string sendOrderDocumentsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/documents";

        // Check/amend manadatory order documents
        const string jsonContentWithSingleQuotes = @"{
            `order`: {
                `purchaseOrderNumber`: `PO0123456789`,
                `documents`: [
                    {
                        `code`: `123456789`,
                        `revision`: `B`,
                        `name`: `Tradecloud API Manual`,
                        `objectId`: `a4f73172-6ccc-4588-8a1d-550297156c9d`,
                        `type`: `General`,
                        `description`: `General document`
                    }
                    ]
                },
                `lines`: [
                    {
                        `position`: `0001`,
                        `documents`: [
                            {
                            `code`: `123456789`,
                            `revision`: `B`,
                            `name`: `Tradecloud API Manual`,
                            `objectId`: `a4f73172-6ccc-4588-8a1d-550297156c9d`,
                            `type`: `General`,
                            `description`: `General document`
                            }
                        ]
                    }
                ]

            }
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order documents example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await SendOrder(accessToken);

            async Task SendOrder(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                // future extension: var jsonContent = JsonConvert.SerializeObject(order);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var startDateTime = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderDocumentsUrl, content);
                watch.Stop();
                Console.WriteLine("SendOrderDocuments start: " + startDateTime +  " elapsed: " + watch.ElapsedMilliseconds + " StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrderDocuments Body: " +  responseString);  
            }
        }
    }
}
