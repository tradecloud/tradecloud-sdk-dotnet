namespace Com.Tradecloud1.SDK.SendOrderResponseSearchBatch;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// WARN: this script will confirm order lines, which cannot be reverted. 
class SendOrderResponseSearchBatch
{
    const bool dryRun = true;
    const string buyerId = "";
    const string buyerAccountNumber = "";
    const string accessToken = "";
    const string orderLineSearchUrl = "https://api.accp.tradecloud1.com/v2/order-line-search/search";
    const string sendOrderResponseUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order-response";

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
                            orderLine.DeliverySchedule[1].Position == "0001" &&
                            orderLine.DeliverySchedule[0].Date == orderLine.DeliverySchedule[1].Date &&
                            orderLine.DeliverySchedule[0].Quantity == orderLine.DeliverySchedule[1].Quantity)
                            {
                                var orderResponse = new OrderResponse
                                {
                                    Order = new OrderResponseOrder
                                    {
                                        CompanyId = orderLine.SupplierOrder.CompanyId,
                                        BuyerAccountNumber = buyerAccountNumber,
                                        PurchaseOrderNumber = orderLine.BuyerOrder.purchaseOrderNumber
                                    },
                                    Lines = new List<OrderResponseLine> {
                                       new OrderResponseLine
                                       {
                                            PurchaseOrderLinePosition = orderLine.BuyerLine.Position,
                                            SalesOrderNumber = orderLine.SupplierLine.SalesOrderNumber,
                                            SalesOrderLinePosition = orderLine.SupplierLine.Position,
                                            DeliverySchedule = new List<DeliveryLine> {
                                                new DeliveryLine
                                                {
                                                    Position = "0",
                                                    Date = orderLine.DeliverySchedule[0].Date,
                                                    Quantity = orderLine.DeliverySchedule[0].Quantity
                                                }
                                            },
                                            Prices = orderLine.Prices,
                                            Reason = "Merged duplicate delivery lines."
                                       }
                                    }
                                };

                                //if (orderLine.BuyerOrder.purchaseOrderNumber == "2208525" && orderLine.BuyerLine.Position == "2")
                                //{
                                await log.WriteLineAsync("SendOrderResponse orderLine: " + JsonConvert.SerializeObject(orderLine));
                                await SendOrderResponse(orderResponse, log);
                                //}
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
            //await log.WriteLineAsync("SearchOrderLines start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            //await log.WriteLineAsync("SearchOrderLines request body=" + query);
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

        async Task SendOrderResponse(OrderResponse orderResponse, StreamWriter log)
        {
            var requestJson = JsonConvert.SerializeObject(orderResponse, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            if (dryRun)
            {
                await log.WriteLineAsync("SendOrderResponse requestJson: " + requestJson);
            }
            else
            {
                var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderResponseUrl, requestContent);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("SendOrderResponse request: " + requestJson + ", start: " + start + ", elapsed: " + watch.ElapsedMilliseconds + "ms, status: " + statusCode + ", reason: " + response.ReasonPhrase);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (statusCode != 200)
                    await log.WriteLineAsync("SendOrderResponse response: " + responseContent);
            }
        }
    }
}
