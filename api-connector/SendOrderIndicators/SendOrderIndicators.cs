using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrderIndicators
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderIndicatorsByBuyerRoute
        const string sendOrderIndicatorsUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order/indicators";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = @"{
            `order`: {
                `purchaseOrderNumber`: `PO0123456789`,
                `indicators`: {
                    `noDeliveryExpected`: false,
                    `shipped`: false,
                    `delivered`: false,
                    `completed`: false,
                    `cancelled` : false
                }
            },
            `lines`: [
                {
                    `position`: `0001`,
                    `indicators`: {
                        `noDeliveryExpected`: false,
                        `shipped`: false,
                        `delivered`: false,
                        `completed`: false,
                        `cancelled` : false
                    }
                }
            ]
        }";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order indicators example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await SendOrderIndicators();

            async Task SendOrderIndicators()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderIndicatorsUrl, content);
                watch.Stop();
                Console.WriteLine("SendOrderIndicators start: " + start +  " elapsed: " + watch.ElapsedMilliseconds + " StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrderIndicators Body: " +  responseString);  
            }
        }
    }
}
