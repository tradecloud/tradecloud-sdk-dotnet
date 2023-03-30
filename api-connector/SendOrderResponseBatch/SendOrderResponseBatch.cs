using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // WARN: this script will confirm order lines, which cannot be reverted. 
    class SendOrderResponseBatch
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string accessToken = "";

       // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-line-search/private/specs.yaml#/order-line-search/getByIdRoute
        const string orderLineSearchUrlTemplate = "https://api.accp.tradecloud1.com/v2/order-line-search/{orderLineId}";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/conversation/private/specs.yaml#/conversation/createOrderConversation
        const string sendOrderResponseUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order-response";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud order response batch.");

            var jsonOrderResponseTemplate = File.ReadAllText(@"order-response-template.json");
            string[] orderLineConfirmations = System.IO.File.ReadAllLines(@"order-line-confirmations.csv");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("order-response-batch.log", append: true))
            {
                var count = 0;
                foreach (var orderLineConfirmation in orderLineConfirmations)
                {
                    var orderLineConfirmationFields = orderLineConfirmation.Split(",");

                    if (count > 0 && orderLineConfirmationFields.Length == 7)
                    {
                        var orderResponse = new OrderResponse 
                        {
                            PurchaseOrderNumber = orderLineConfirmationFields[0],
                            PurchaseOrderLinePosition = orderLineConfirmationFields[1],
                            DeliveryLinePosition = orderLineConfirmationFields[2],
                            ConfirmedDate = orderLineConfirmationFields[3],
                            ConfirmedQuantity = orderLineConfirmationFields[4],
                            ConfirmedNetPrice = orderLineConfirmationFields[5],
                            Shipped = orderLineConfirmationFields[6],
                        };

                        var lineId = buyerId + "-" + orderResponse.PurchaseOrderNumber + "-" + orderResponse.PurchaseOrderLinePosition;
                        var queryResult = await FindOrderLineById(lineId, log);
                        if (queryResult != null)
                        {
                            string foundLineId = queryResult["id"].ToString();
                            string foundOrderId = queryResult["orderId"].ToString();
                            string foundBuyerId = queryResult["buyerOrder"]["companyId"].ToString();
                            string foundSupplierId = queryResult["supplierOrder"]["companyId"].ToString();
                            await log.WriteLineAsync("Order line found lineId=" + foundLineId + " orderId=" + foundOrderId + " buyerId=" + foundBuyerId + " supplierId=" + foundSupplierId);
                            
                            if (dryRun)
                            {
                                await log.WriteLineAsync("SendOrderResponse dry run: lineId=" + foundOrderId);
                            }
                            else
                            {
                                //await SendOrderResponse(orderResponse, log);
                            }
                        }
                    }
                    else if (orderLineConfirmationFields.Length != 7)
                    {
                        await log.WriteLineAsync("SendOrderResponse skipped: " + orderLineConfirmation);
                    }

                    count++;
                }

                async Task<JObject> FindOrderLineById(string orderLineId, StreamWriter log)
                {                
                    var orderLineSearchUrl = orderLineSearchUrlTemplate.Replace("{orderLineId}", orderLineId);

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(orderLineSearchUrl);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("FindOrderLineById orderLineId=" + orderLineId + " start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode == 200)
                    {
                        await log.WriteLineAsync("FindOrderById response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                        return JObject.Parse(responseString);
                    }
                    else
                    {
                        await log.WriteLineAsync("FindOrderById response body=" +  responseString);
                        return null;
                    }
                }

                async Task SendOrderResponse(OrderResponse orderResponse, StreamWriter log)
                {           
                    var jsonOrderResponse = jsonOrderResponseTemplate;
                     //   .Replace("{companyId}", orderResponse.

                    var content = new StringContent(jsonOrderResponse, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderResponseUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    Console.WriteLine("SendOrderResponse start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    if (statusCode == 400)
                        Console.WriteLine("SendOrderResponse request body=" + jsonOrderResponse); 
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                        Console.WriteLine("SendOrderResponse response body=" +  responseString);
                }
            }
        }
    }

    public class OrderResponse
    {
        public string CompanyId { get; set; }
        public string BuyerAccountNumber { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string PurchaseOrderLinePosition { get; set; } 
        public string DeliveryLinePosition { get; set; }
        public string ConfirmedDate { get; set; }
        public string ConfirmedQuantity { get; set; }
        public string ConfirmedNetPrice { get; set; }
        public string CurrencyIso { get; set; }
        public string PriceUnitOfMeasureIso { get; set; }
        public string PriceUnitQuantity { get; set; }
        public string Shipped { get; set; }
    }
}
