namespace Com.Tradecloud1.SDK.SendOrderResponseSearchBatch;

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

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

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using (var log = new StreamWriter("send_order_response_search_batch.log", append: true))
        {
            int offset = 0;
            int total = limit;
            while (total > offset && offset < maxTotal)
            {
                var orderLineSearchView = await SearchOrderLines(offset, log);
                if (orderLineSearchView != null)
                {
                    total = orderLineSearchView.Total;
                    await log.WriteLineAsync("total=" + total + " offset=" + offset);
                    offset += limit;

                    foreach (var orderLine in orderLineSearchView.Data)
                    {
                        if (orderLine.Status.ProcessStatus == "Confirmed" &&
                            orderLine.Status.LogisticsStatus == "Open" &&
                            orderLine.DeliverySchedule.Count == 2)
                        {
                            if (orderLine.DeliverySchedule[0].Position == null &&
                            orderLine.DeliverySchedule[1].Position == null &&
                            orderLine.DeliverySchedule[0].Date == orderLine.DeliverySchedule[1].Date &&
                            orderLine.DeliverySchedule[0].Quantity == orderLine.DeliverySchedule[1].Quantity)
                            {
                                if (dryRun)
                                {
                                    await log.WriteLineAsync("orderLine: " + JsonConvert.SerializeObject(orderLine));
                                }
                                else
                                {
                                    // var orderResponse = new OrderResponse {
                                    //     Order =  new OrderResponseOrder {

                                    //     },
                                    //     Lines = new OrderResponseLine 
                                    // }        

                                    //await SendOrderResponse(purchaseOrderNumber, position, log);
                                }
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

        async Task<OrderLineSearchView> SearchOrderLines(int offset, StreamWriter log)
        {
            var queryTemplate = queryTemplateWithSingleQuotes.Replace("'", "\"");
            var query = queryTemplate
                .Replace("{buyerId}", buyerId)
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
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == 200)
            {
                //await log.WriteLineAsync("SearchOrderLines response content=" + responseContent);
                return JsonConvert.DeserializeObject<OrderLineSearchView>(responseContent);
            }
            else
            {
                await log.WriteLineAsync("SearchOrderLines response content=" + responseContent);
                return null;
            }
        }

        // async Task SendOrderResponse(OrderLineResponse orderLineResponse, StreamWriter log)
        // {
        //     var jsonOrderResponse = jsonOrderResponseTemplate
        //         .Replace("{companyId}", orderLineResponse.companyId)
        //         .Replace("{buyerAccountNumber}", orderLineResponse.buyerAccountNumber)
        //         .Replace("{purchaseOrderNumber}", orderLineResponse.purchaseOrderNumber)
        //         .Replace("{purchaseOrderLinePosition}", orderLineResponse.purchaseOrderLinePosition)
        //         .Replace("{deliveryLinePosition}", orderLineResponse.deliveryLinePosition)
        //         .Replace("{confirmedDate}", orderLineResponse.confirmedDate)
        //         .Replace("{confirmedQuantity}", orderLineResponse.confirmedQuantity)
        //         .Replace("{confirmedNetPriceValue}", orderLineResponse.confirmedNetPrice)
        //         .Replace("{confirmedNetPriceCurrencyIso}", orderLineResponse.currencyIso)
        //         .Replace("{priceUnitOfMeasureIso}", orderLineResponse.priceUnitOfMeasureIso)
        //         .Replace("{priceUnitQuantity}", orderLineResponse.priceUnitQuantity);

        //     if (dryRun)
        //     {
        //         await log.WriteLineAsync("SendOrderResponse dry run jsonOrderResponse=" + jsonOrderResponse);
        //     }
        //     else
        //     {
        //         var content = new StringContent(jsonOrderResponse, Encoding.UTF8, "application/json");

        //         var start = DateTime.Now;
        //         var watch = System.Diagnostics.Stopwatch.StartNew();
        //         var response = await httpClient.PostAsync(sendOrderResponseUrl, content);
        //         watch.Stop();

        //         var statusCode = (int)response.StatusCode;
        //         await log.WriteLineAsync("SendOrderResponse orderLineId=" + orderLineResponse.orderLineId + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
        //         if (statusCode == 400)
        //             await log.WriteLineAsync("SendOrderResponse request body=" + jsonOrderResponse);
        //         string responseString = await response.Content.ReadAsStringAsync();
        //         if (statusCode != 200)
        //             await log.WriteLineAsync("SendOrderResponse response body=" + responseString);
        //     }
        // }
    }
}
