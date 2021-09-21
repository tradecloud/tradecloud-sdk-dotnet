using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ConfirmOrderLine
    {   
        // Fill in the mandatory username
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // Fill in the order LINE id (UUID) you got when fetching the order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/confirmOrderLineById
        const string confirmPurchaseOrderLineUrl = "https://accp.tradecloud.nl/api/v1/purchaseOrderLine/{purchaseOrderLineId}/_confirm";
        
        // Fill in the confirmed values
        // - quantityConfirmed is mandatory
        // - deliveryDateConfirmed is mandatory
        // - either use grossPurchasePriceConfirmed with discountPercentageConfirmed
        // - or use netPurchasePriceConfirmed
        // - supplierOrderDescription and supplierLineDescription are optional
        // - explanation is optional when confirming with inconsistent values
        const string jsonContentWithSingleQuotes = @"{
             'quantityConfirmed': 1.0,
             'deliveryDateConfirmed': '2021-09-21',
             'grossPurchasePriceConfirmed': {
                 'value': 10.0,
                 'currency': 'EUR'
             },
             'discountPercentageConfirmed': 50,
             'netPurchasePriceConfirmed': {
                 'value': 5.0,
                 'currency': 'EUR'
            },
            'priceUnitQuantityConfirmed': 1.0,
            'supplierOrderDescription': '<supplierOrderDescription>',  
            'supplierLineDescription': '<supplierLineDescription>',
            'explanation': '<explanation>'
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud confirm order line example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            await ConfirmOrderLine();

            async Task ConfirmOrderLine()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(confirmPurchaseOrderLineUrl, content);
                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ConfirmOrderLine status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("ConfirmOrderLine response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("ConfirmOrderLine response body=" +  responseString);
            }
        }
    }
}
