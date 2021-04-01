using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderDeliveries
    {   
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderDeliveriesByBuyer
        const string sendOrderDeliveriesUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/deliveries";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = @"{
            `order`: {
                `purchaseOrderNumber`: `TC-6570-SCI-1`
            },
            `lines`: [
                {
                    `position`: `01`,
                    `deliveries`: [
                        {
                            `position`: `01`,
                            `date`: `2020-12-24`,
                            `quantity`: 41.5
                        },
                        {
                            `position`: `02`,
                            `date`: `2020-12-25`,
                            `quantity`: 41.5
                        }
                     ]
                }
            ],
            `erpLastChangeDateTime`: `2019-12-31T10:11:12`,
            `erpLastChangedBy`: {
                `email`: `contact@yourcompany.com`
            }
        }";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order deliveries using basic authentication example.");
            
            HttpClient httpClient = new HttpClient();
            await SendOrderDeliveries();

            async Task SendOrderDeliveries()
            {                
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderDeliveriesUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrderDeliveries start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SendOrderDeliveries request body=" + jsonContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrderDeliveries response body=" +  responseString);  
            }
        }
    }
}
