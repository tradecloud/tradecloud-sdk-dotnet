using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    // WARN: this script will confirm order lines, which cannot be reverted. 
    class SendOrderResponseBatch
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string buyerAccountNumber = "";
        const string accessToken = "";

       // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-line-search/private/specs.yaml#/order-line-search/getByIdRoute
        const string orderLineSearchUrlTemplate = "https://api.accp.tradecloud1.com/v2/order-line-search/{orderLineId}";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/conversation/private/specs.yaml#/conversation/createOrderConversation
        const string sendOrderResponseUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order-response";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud order response batch.");

            var jsonOrderResponseTemplate = File.ReadAllText(@"order-response-template.json");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("order-response-batch.log", append: true))
            using (var reader = new StreamReader("order-line-confirmations.csv"))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                
                csvReader.Context.RegisterClassMap<OrderLineResponseMap>();
                var orderLineResponses = csvReader.GetRecords<OrderLineResponse>();
                foreach (var orderLineResponse in orderLineResponses)
                {
                    var lineId = buyerId + "-" + orderLineResponse.PurchaseOrderNumber + "-" + orderLineResponse.PurchaseOrderLinePosition;
                    var queryResult = await FindOrderLineById(lineId, log);
                    if (queryResult != null)
                    {
                        orderLineResponse.companyId = queryResult["supplierOrder"]["companyId"].ToString();
                        orderLineResponse.buyerAccountNumber = buyerAccountNumber;
                        orderLineResponse.currencyIso = queryResult["buyerLine"]["prices"]["netPrice"]["priceInBaseCurrency"]["currencyIso"].ToString();
                        orderLineResponse.priceUnitOfMeasureIso = queryResult["buyerLine"]["prices"]["priceUnitOfMeasureIso"].ToString();
                        orderLineResponse.priceUnitQuantity = queryResult["buyerLine"]["prices"]["priceUnitQuantity"].ToString();

                        await log.WriteLineAsync("orderLineResponse=" + JsonConvert.SerializeObject(orderLineResponse));
                        
                        if (dryRun)
                        {
                            await log.WriteLineAsync("SendOrderResponse dry run: orderLineResponse=" + orderLineResponse);
                        }
                        else
                        {
                            //await SendOrderResponse(orderResponse, log);
                        }
                    }
                }
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
                    //await log.WriteLineAsync("FindOrderById response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                    return JObject.Parse(responseString);
                }
                else
                {
                    await log.WriteLineAsync("FindOrderById response body=" +  responseString);
                    return null;
                }
            }

            async Task SendOrderResponse(OrderLineResponse orderLineResponse, StreamWriter log)
            {           
                var jsonOrderResponse = jsonOrderResponseTemplate
                    .Replace("{companyId}", orderResponse.companyId)
                    .Replace("{buyerAccountNumber}", orderResponse.buyerAccountNumber)
                    .Replace("{purchaseOrderNumber}", orderResponse.purchaseOrderNumber)
                    .Replace("{purchaseOrderLinePosition}", orderResponse.purchaseOrderLinePosition)
                    .Replace("{confirmedDeliveryDate}", orderResponse.confirmedDeliveryDate)
                    .Replace("{confirmedQuantity}", orderResponse.confirmedQuantity)
                    .Replace("{confirmedNetPrice}", orderResponse.confirmedNetPrice)
                    .Replace("{currencyIso}", orderResponse.currencyIso)
                    .Replace("{priceUnitOfMeasureIso}", orderResponse.priceUnitOfMeasureIso)
                    .Replace("{priceUnitQuantity}", orderResponse.priceUnitQuantity);

                var content = new StringContent(jsonOrderResponse, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderResponseUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrderResponse start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                    Console.WriteLine("SendOrderResponse request body=" + jsonOrderResponse); 
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200)
                    Console.WriteLine("SendOrderResponse response body=" +  responseString);
            }
        }
    }

    public class OrderLineResponse
    {
        public string companyId { get; set; }
        public string buyerAccountNumber { get; set; }
        public string purchaseOrderNumber { get; set; }
        public string purchaseOrderLinePosition { get; set; } 
        public string deliveryLinePosition { get; set; }
        public string confirmedDeliveryDate { get; set; }
        public string confirmedQuantity { get; set; }
        public string confirmedNetPrice { get; set; }
        public string currencyIso { get; set; }
        public string priceUnitOfMeasureIso { get; set; }
        public string priceUnitQuantity { get; set; }
    }

    public sealed class OrderLineResponseMap : ClassMap<OrderLineResponse>
    {
        public OrderLineResponseMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.CompanyId).Ignore();
            Map(m => m.BuyerAccountNumber).Ignore();
            Map(m => m.deliveryLinePosition).Ignore();
            Map(m => m.CurrencyIso).Ignore();
            Map(m => m.PriceUnitOfMeasureIso).Ignore();
            Map(m => m.PriceUnitQuantity).Ignore();
        }
    }
}
