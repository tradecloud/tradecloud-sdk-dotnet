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
        const bool useToken = true;
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
            'order': {
                'supplierAccountNumber': '540830',
                'purchaseOrderNumber': 'PO0123456789',
                'description': 'Any buyer custom text about this order',
                'destination': {
                    'code': '001',
                    'names': [
                        'My Company Warehouse',
                        'Dock 12'
                    ],
                    'addressLines': [
                        'Street 123',
                        'Area 52'
                    ],
                    'postalCode': '1234 AB',
                    'city': 'Rotterdam',
                    'countryCodeIso2': 'NL',
                    'countryName': 'the Netherlands',
                    'locationType': 'warehouse'
                },
                'terms': {
                    'incotermsCode': 'CIF',
                    'incoterms': 'ECT Rotterdam',
                    'paymentTermsCode': '30D',
                    'paymentTerms': '30 days'
                },
                'indicators': {
                    'noDeliveryExpected': false,
                    'shipped': false,
                    'delivered': false,
                    'completed': false
                },
                'properties': [
                    {
                        'key': 'color',
                        'value': 'red'
                    }
                ],
                'documents': [
                    {
                        'code': '123456789',
                        'revision': 'B',
                        'name': 'Tradecloud API Manual',
                        'objectId': 'a4f73172-6ccc-4588-8a1d-550297156c9d',
                        'type': 'General',
                        'description': 'General document'
                    }
                ],
                'notes': [
                    'one order note',
                    'another order note'
                ],
                'labels': [
                    'one order label',
                    'another order label',
                    'third order label'
                ],
                'contact': {
                    'email': 'frankjan@tradecloud1.com'
                }
            },
            'lines': [
                {
                    'position': '0001',
                    'row': '1',
                    'description': 'Any buyer text about this line',
                    'item': {
                        'number': '12345',
                        'revision': 'v2',
                        'name': 'Round tube ø60x45',
                        'description': 'Very nice round tube ø60 x 45',
                        'purchaseUnitOfMeasureIso': 'PCE',
                        'supplierItemNumber': '67890'
                    },
                    'itemDetails': {
                        'countryOfOriginCodeIso2': 'NL',
                        'combinedNomenclatureCode': '6406 10 10',
                        'netWeight': 41.241,
                        'netWeightUnitOfMeasureIso': 'KG',
                        'dangerousGoodsCodeUnece': '0060',
                        'serialNumber': '0x0000000000028000',
                        'batchNumber': '#18001'
                    },
                    'deliverySchedule': [
                          {
                            'position': '0001',
                            'date': '2021-11-01',
                            'quantity': 1234.56
                        },
                        {
                            'position': '0002',
                            'date': '2021-11-02',
                            'quantity': 1234.56
                        },
                        {
                            'position': '0003',
                            'date': '2021-11-03',
                            'quantity': 1234.56
                        }
                    ],
                    'prices': {
                        'grossPrice': {
                            'priceInTransactionCurrency': {
                                'value': 1234.56,
                                'currencyIso': 'EUR'
                            },
                            'priceInBaseCurrency': {
                                'value': 1234.56,
                                'currencyIso': 'EUR'
                            }
                        },
                        'discountPercentage': 50,
                        'netPrice': {
                            'priceInTransactionCurrency': {
                                'value': 1234.56,
                                'currencyIso': 'EUR'
                            },
                            'priceInBaseCurrency': {
                                'value': 1234.56,
                                'currencyIso': 'EUR'
                            }
                        },
                        'priceUnitOfMeasureIso': 'PCE',
                        'priceUnitQuantity': 100
                    },                 
                    'terms': {
                        'contractNumber': '123456789',
                        'contractPosition': '0001'
                    },
                    'projectNumber': 'PROJ12345',
                    'productionNumber': 'PROD12345',
                    'salesOrderNumber': 'SO123456789',
                    'indicators': {
                        'noDeliveryExpected': false,
                        'shipped': false,
                        'delivered': false,
                        'completed': false
                    },
                    'properties': [
                        {
                        'key': 'color',
                        'value': 'red'
                        }
                    ],
                    'documents': [
                        {
                            'code': '123456789',
                            'revision': 'B',
                            'name': 'Tradecloud API Manual',
                            'objectId': 'a4f73172-6ccc-4588-8a1d-550297156c9d',
                            'type': 'General',
                            'description': 'General document'
                        }
                    ],
                    'notes': [
                        'one line note',
                        'another line note'
                    ],
                    'labels': [
                        'one line label',
                        'another line label'
                    ]
                }
            ],
            'erpIssueDateTime': '2019-12-31T10:11:12',
            'erpIssuedBy': {
                'email': 'frankjan@tradecloud1.com'
            },
            'erpLastChangeDateTime': '2019-12-31T10:11:12',
            'erpLastChangedBy': {
                'email': 'contact@yourcompany.com'
            }
        }";        
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send order example.");
            
            HttpClient httpClient = new HttpClient();
            if (useToken)
            {
                var authenticationClient = new Authentication(httpClient, authenticationUrl);
                var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            
            }
            else
            {
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
            }
            await SendOrder();

            async Task SendOrder()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(sendOrderUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrder start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SendOrder request body=" + jsonContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("SendOrder response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("SendOrder response body=" +  responseString);
            }
        }
    }
}
