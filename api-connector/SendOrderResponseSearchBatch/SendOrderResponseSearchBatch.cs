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
    class SendOrderResponseSearchBatch
    {
        const bool dryRun = true;
        const string buyerId = "";
        const string buyerAccountNumber = "";
        const string accessToken = "";
        const string orderLineSearchUrl = "https://api.tradecloud1.com/v2/order-line-search/search";
        const string sendOrderResponseUrl = "https://api.tradecloud1.com/v2/api-connector/order-response";

        // Fill in the search query
        const string queryTemplateWithSingleQuotes = @"{
            'filters': {
                'buyerOrder': {
                    'companyId': ['{buyerId}']
                },         
                'status': {
                    'processStatus': ['Confirmed'],
                    'logisticsStatus': ['Open']
                }
            },
            'sort':[{'field':'buyerOrder.purchaseOrderNumber','order':'asc'}],
            'offset': {offset},
            'limit': {limit}
        }";
        const int limit = 100;
        const int maxTotal = 10000;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud order response, order line search based batch.");
            var jsonOrderIndicatorsTemplate = File.ReadAllText(@"order-indicators-template.json");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using (var log = new StreamWriter("send_order_lines_indicators_search_batch.log", append: true))
            {
                int offset = 0;
                int total = limit;
                while (total > offset && offset < maxTotal)
                {
                    var queryResult = await SearchOrderLines(offset, log);
                    if (queryResult != null)
                    {
                        total = ((int)queryResult["total"]);
                        await log.WriteLineAsync("total=" + total + " offset=" + offset);
                        offset += limit;

                        foreach (var orderLine in queryResult.First.Values())
                        {
                            string processStatus = orderLine["status"]["processStatus"].ToString();
                            string logisticsStatus = orderLine["status"]["logisticsStatus"].ToString();

                            if (processStatus == "Confirmed" && logisticsStatus == "Open")
                            {
                                string supplierId = orderLine["supplierOrder"]["companyId"].ToString();
                                string purchaseOrderNumber = orderLine["buyerOrder"]["purchaseOrderNumber"].ToString();
                                string purchaseOrderLinePosition = orderLine["buyerLine"]["position"].ToString();
                                string deliveryLinePos1 = orderLine["deliverySchedule"]["position"].ToString();

                                if

                                var orderLineResponse = new OrderLineResponse
                                {
                                    companyId = supplierId,
                                    buyerAccountNumber = buyerAccountNumber.
                                    purchaseOrderNumber,
                                    purchaseOrderLinePosition,
                                    deliveryLinePosition = "0",
        public string confirmedDate { get; set; }
        public string confirmedQuantity { get; set; }
        public string confirmedNetPrice { get; set; }
        public string currencyIso { get; set; }
        public string priceUnitOfMeasureIso { get; set; }
        public string priceUnitQuantity { get; set; }

    }    


                                if (dryRun) 
                                {
                                    await log.WriteLineAsync("supplierId=" + supplierId + " purchaseOrderNumber=" + purchaseOrderNumber + " position=" + position + " processStatus=" + processStatus + " logisticsStatus=" + logisticsStatus);
}
                                else
{
    await SendOrderResponse(purchaseOrderNumber, position, log);
}
                            }
                        }
                    }
                    else
{
    total = 0;
}
                }
            }

            async Task<JObject> SearchOrderLines(int offset, StreamWriter log)
{
    var queryTemplate = queryTemplateWithSingleQuotes.Replace("'", "\"");
    var query = queryTemplate
        .Replace("{buyerId}", buyerId)
        .Replace("{supplierId}", supplierId)
        .Replace("{offset}", offset.ToString())
        .Replace("{limit}", limit.ToString());
    var content = new StringContent(query, Encoding.UTF8, "application/json");

    var start = DateTime.Now;
    var watch = System.Diagnostics.Stopwatch.StartNew();
    var response = await httpClient.PostAsync(orderLineSearchUrl, content);
    watch.Stop();

    var statusCode = (int)response.StatusCode;
    await log.WriteLineAsync("SearchOrderLines start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
    await log.WriteLineAsync("SearchOrderLines request body=" + query);
    string responseString = await response.Content.ReadAsStringAsync();
    if (statusCode == 200)
    {
        return JObject.Parse(responseString);
    }
    else
    {
        await log.WriteLineAsync("SearchOrderLines response body=" + responseString);
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
            await log.WriteLineAsync("SendOrderResponse response body=" + responseString);
    }
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
    public string confirmedDate { get; set; }
    public string confirmedQuantity { get; set; }
    public string confirmedNetPrice { get; set; }
    public string currencyIso { get; set; }
    public string priceUnitOfMeasureIso { get; set; }
    public string priceUnitQuantity { get; set; }
}
}
