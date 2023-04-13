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
        const string delimiter = ";";
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

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter, Encoding = Encoding.UTF8 };

            using (var log = new StreamWriter("order-response-batch.log", append: true))
            using (var reader = new StreamReader("order-line-confirmations.csv"))
            using (var csvReader = new CsvReader(reader, config))
            {
                csvReader.Context.RegisterClassMap<OrderLineResponseMap>();
                var orderLineResponses = csvReader.GetRecords<OrderLineResponse>();
                foreach (var orderLineResponse in orderLineResponses)
                {
                    var orderLineId = buyerId + "-" + orderLineResponse.purchaseOrderNumber + "-" + orderLineResponse.purchaseOrderLinePosition;
                    var queryResult = await FindOrderLineById(orderLineId, log);
                    if (queryResult != null)
                    {
                        orderLineResponse.orderLineId = orderLineId;
                        orderLineResponse.companyId = queryResult["supplierOrder"]["companyId"].ToString();
                        orderLineResponse.buyerAccountNumber = buyerAccountNumber;
                        orderLineResponse.deliveryLinePosition = orderLineResponse.purchaseOrderLinePosition;
                        if (orderLineResponse.confirmedDate.Contains("/"))
                        {
                            var confirmedDateTime = DateTime.ParseExact(orderLineResponse.confirmedDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            orderLineResponse.confirmedDate = confirmedDateTime.ToString("yyyy-MM-dd");
                        }
                        orderLineResponse.confirmedNetPrice = orderLineResponse.confirmedNetPrice.Replace(",", ".");
                        orderLineResponse.currencyIso = queryResult["buyerLine"]["prices"]["netPrice"]["priceInBaseCurrency"]["currencyIso"].ToString();
                        orderLineResponse.priceUnitOfMeasureIso = queryResult["buyerLine"]["prices"]["priceUnitOfMeasureIso"].ToString();
                        orderLineResponse.priceUnitQuantity = queryResult["buyerLine"]["prices"]["priceUnitQuantity"].ToString();

                        await log.WriteLineAsync("SendOrderResponse orderLineResponse=" + JsonConvert.SerializeObject(orderLineResponse));
                        await SendOrderResponse(orderLineResponse, log);
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
                    await log.WriteLineAsync("FindOrderLineById orderLineId=" + orderLineId + " response body=" +  responseString);
                    return null;
                }
            }

            async Task SendOrderResponse(OrderLineResponse orderLineResponse, StreamWriter log)
            {           
                var jsonOrderResponse = jsonOrderResponseTemplate
                    .Replace("{companyId}", orderLineResponse.companyId)
                    .Replace("{buyerAccountNumber}", orderLineResponse.buyerAccountNumber)
                    .Replace("{purchaseOrderNumber}", orderLineResponse.purchaseOrderNumber)
                    .Replace("{purchaseOrderLinePosition}", orderLineResponse.purchaseOrderLinePosition)
                    .Replace("{deliveryLinePosition}", orderLineResponse.deliveryLinePosition)
                    .Replace("{confirmedDate}", orderLineResponse.confirmedDate)
                    .Replace("{confirmedQuantity}", orderLineResponse.confirmedQuantity)
                    .Replace("{confirmedNetPriceValue}", orderLineResponse.confirmedNetPrice)
                    .Replace("{confirmedNetPriceCurrencyIso}", orderLineResponse.currencyIso)
                    .Replace("{priceUnitOfMeasureIso}", orderLineResponse.priceUnitOfMeasureIso)
                    .Replace("{priceUnitQuantity}", orderLineResponse.priceUnitQuantity);
                
                if (dryRun)
                {
                    await log.WriteLineAsync("SendOrderResponse dry run jsonOrderResponse=" + jsonOrderResponse);
                }
                else
                {
                    var content = new StringContent(jsonOrderResponse, Encoding.UTF8, "application/json");

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(sendOrderResponseUrl, content);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("SendOrderResponse orderLineId=" + orderLineResponse.orderLineId + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                    if (statusCode == 400)
                        await log.WriteLineAsync("SendOrderResponse request body=" + jsonOrderResponse); 
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (statusCode != 200)
                        await log.WriteLineAsync("SendOrderResponse response body=" +  responseString);
                }
            }
        }
    }

    public class OrderLineResponse
    {
        public string orderLineId { get; set; }
        public string companyId { get; set; }
        public string buyerAccountNumber { get; set; }
        public string purchaseOrderNumber { get; set; }
        public string purchaseOrderLinePosition { get; set; } 
        public string deliveryLinePosition { get; set; }
        public string confirmedDate { get; set; }
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
            Map(m => m.orderLineId).Ignore();
            Map(m => m.companyId).Ignore();            
            Map(m => m.buyerAccountNumber).Ignore();
            Map(m => m.deliveryLinePosition).Ignore();
            Map(m => m.currencyIso).Ignore();
            Map(m => m.priceUnitOfMeasureIso).Ignore();
            Map(m => m.priceUnitQuantity).Ignore();
        }
    }
}
