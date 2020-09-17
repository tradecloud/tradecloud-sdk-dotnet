using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://tc-4220-supplier-order-response.test.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "kramp-integration@tradecloud1.com";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/supplier-endpoints/sendOrderResponseBySupplierRoute
        const string sendOrderResponseUrl = "https://tc-4220-supplier-order-response.test.tradecloud1.com/v2/api-connector/order-response";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = @"{
            `order`: {
                `companyId`: `1f61e695-7545-5670-9384-58e8b1f263e6`,
                `buyerAccountNumber`: `1000`,
                `purchaseOrderNumber`: `PO-Marcel-1`,
                `description`: `Any supplier custom text about this order`,                
                `indicators`: {
                    `accepted`: false,
                    `rejected`: false,
                    `shipped`: false,
                    `cancelled`: false
                },
                `properties`: [
                    {
                        `key`: `color`,
                        `value`: `red`
                    }
                ],
                `documents`: [],
                `notes`: [
                    `one note`,
                    `another note`
                ],
                `contact`: {
                    `email`: `eric@tradecloud1.com`
                }
            },
            `lines`: [
                {
                    `purchaseOrderLinePosition`: `0001`,
                    `salesOrderNumber`: `SO123456789`,
                    `salesOrderLinePosition`: `0001`,
                    `description`: `Any supplier text about this line`,
                    `itemDetails`: {
                        `countryOfOriginCodeIso2`: `NL`,
                        `combinedNomenclatureCode`: `6406 10 10`,
                        `netWeight`: 41.241,
                        `netWeightUnitOfMeasureIso`: `KG`,
                        `dangerousGoodsCodeUnece`: `0060`,
                        `serialNumber`: `0x0000000000028000`,
                        `batchNumber`: `#18001`
                    },
                    `deliverySchedule`: [
                          {
                            `position`: `0001`,
                            `date`: `2021-11-01`,
                            `quantity`: 1234.56
                        }
                    ],
                    `deliveryHistory`: [
                        {
                            `position`: `0002`,
                            `date`: `2021-02-01`,
                            `quantity`: 7890.12
                        }
                    ],
                    `prices`: {
                        `grossPrice`: {
                            `priceInTransactionCurrency`: {
                                `value`: 1234.56,
                                `currencyIso`: `EUR`
                            },
                            `priceInBaseCurrency`: {
                                `value`: 1234.56,
                                `currencyIso`: `EUR`
                            }
                        },
                        `discountPercentage`: 50,
                        `netPrice`: {
                            `priceInTransactionCurrency`: {
                                `value`: 1234.56,
                                `currencyIso`: `EUR`
                            },
                            `priceInBaseCurrency`: {
                                `value`: 1234.56,
                                `currencyIso`: `EUR`
                            }
                        },
                        `priceUnitOfMeasureIso`: `PCE`,
                        `priceUnitQuantity`: 100
                    },
                    `indicators`: {
                        `accepted`: false,
                        `rejected`: false,
                        `shipped`: false,
                        `cancelled`: false
                    },
                    `properties`: [
                        {
                        `key`: `color`,
                        `value`: `red`
                        }
                    ],
                    `documents`: [],
                    `notes`: [
                        `one note`,
                        `another note`
                    ],  
                    `reason`: `Out of stock.`
                }
            ],
            `erpResponseDateTime`: `2019-12-31T10:11:12`,
            `erpRespondedBy`: {
                `email`: `eric@tradecloud1.com`
            }
        }";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order response example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await SendOrderResponse(accessToken);

            async Task SendOrderResponse(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderResponseUrl, content);
                watch.Stop();
                Console.WriteLine("SendOrderResponse StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrderResponse Body: " +  responseString);  
            }
        }
    }
}
