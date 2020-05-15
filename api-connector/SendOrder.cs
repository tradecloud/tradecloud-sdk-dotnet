using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/api-connector/specs.yaml#/buyer-endpoints/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.accp.tradecloud1.com/v2/api-connector/order";

        // Check/amend manadatory order
        const string jsonContentWithSingleQuotes = @"{
            `order`: {
                `companyId`: `f56aa4ce-8ec8-5197-bc26-77716a58add7`,
                `supplierAccountNumber`: `540830`,
                `purchaseOrderNumber`: `PO123456789`,
                `description`: `Any custom text about this order`,
                `destination`: {
                    `code`: `001`,
                    `names`: [
                        `My Company Warehouse`,
                        `Dock 12`
                    ],
                    `addressLines`: [
                        `Street 123`,
                        `Area 52`
                    ],
                    `postalCode`: `1234 AB`,
                    `city`: `Rotterdam`,
                    `countryCodeIso2`: `NL`,
                    `countryName`: `the Netherlands`,
                    `locationType`: `warehouse`
                },
                `terms`: {
                    `incotermsCode`: `CIF`,
                    `incoterms`: `ECT Rotterdam`,
                    `paymentTermsCode`: `30D`,
                    `paymentTerms`: `30 days`
                },
                `indicators`: {
                    `noDeliveryExpected`: false,
                    `shipped`: false,
                    `delivered`: false,
                    `completed`: false
                },
                `properties`: [
                    {
                        `key`: `color`,
                        `value`: `red`
                    }
                ],
                `notes`: [
                    `one note`,
                    `another note`
                ],
                `contact`: {
                    `email`: `frankjan@tradecloud1.com`
                }
            },
            `lines`: [
                {
                    `position`: `0001`,
                    `description`: `Anything about this line`,
                    `item`: {
                        `number`: `12345`,
                        `revision`: `v2`,
                        `name`: `Round tube ø60x45`,
                        `description`: `Very nice round tube ø60 x 45`,
                        `purchaseUnitOfMeasureIso`: `PCE`,
                        `supplierItemNumber`: `67890`
                    },
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
                            `position`: `01`,
                            `date`: `2019-12-31`,
                            `quantity`: 1234.56,
                            `locationType`: `harbor`,
                            `locationName`: `ECT Rotterdam`
                        }
                    ],
                    `deliveryHistory`: [
                        {
                            `position`: `01`,
                            `date`: `2019-12-31`,
                            `quantity`: 1234.56,
                            `locationType`: `harbor`,
                            `locationName`: `ECT Rotterdam`
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
                    `terms`: {
                        `contractNumber`: `123456789`,
                        `contractPosition`: `0001`
                    },
                    `projectNumber`: `PROJ12345`,
                    `productionNumber`: `PROD12345`,
                    `salesOrderNumber`: `SO123456789`,
                    `indicators`: {
                        `noDeliveryExpected`: false,
                        `shipped`: false,
                        `delivered`: false,
                        `completed`: false
                    },
                    `properties`: [
                        {
                        `key`: `color`,
                        `value`: `red`
                        }
                    ],
                    `notes`: [
                        `one note`,
                        `another note`
                    ]
                }
            ],
            `erpIssueDateTime`: `2019-12-31T10:11:12`,
            `erpIssuedBy`: {
                `email`: `frankjan@tradecloud1.com`
            },
            `erpLastChangeDateTime`: `2019-12-31T10:11:12`,
            `erpLastChangedBy`: {
                `email`: `contact@yourcompany.com`
            }
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await SendOrder(accessToken);

            async Task SendOrder(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                // future extension: var jsonContent = JsonConvert.SerializeObject(order);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderUrl, content);
                watch.Stop();
                Console.WriteLine("SendOrder StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrder Body: " +  responseString);  
            }
        }
    }
}
