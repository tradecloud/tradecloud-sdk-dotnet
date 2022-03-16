using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchUsers
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/shipment/specs.yaml#/shipment/searchShipmentsRoute
        const string getUserByEmailUrl = "https://api.accp.tradecloud1.com/v2/shipment/search";

        const string jsonContentWithSingleQuotes = 
            @"{
                `filters`: {
                    `supplierShipment`: {
                        `companyIds`: [
                            <supplierCompanyId>
                        ]
                    },
                    `buyerShipment`: {
                        `companyIds`: [
                            <buyerCompanyId>
                        ]
                    }
                },
                `offset`: 0,
                `limit`: 10
            }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud search shipments example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken)  = await authenticationClient.Login(username, password);
            await SearchShipments(accessToken);

            async Task SearchShipments(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(getUserByEmailUrl, content);
                watch.Stop();
                Console.WriteLine("SearchShipments StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SearchShipments Body: " +  responseString);  
            }
        }
    }
}
