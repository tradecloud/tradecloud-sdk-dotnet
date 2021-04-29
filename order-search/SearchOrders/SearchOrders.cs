﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class SearchOrders
    {   
        const bool useToken = true;
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/specs.yaml#/order-search
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/search";
        
        // Fill in the search query
        const string jsonContentWithSingleQuotes = @"{
            'query': 'string',
            'filters': {
                'buyerOrder': {
                    'companyId': [
                        '3fa85f64-5717-4562-b3fc-2c963f66afa6'
                    ],
                    'contact': {
                        'userIds': [
                        '3fa85f64-5717-4562-b3fc-2c963f66afa6'
                        ]
                    },
                    'destination': {
                        'code': [
                            'string'
                        ]
                    }
                },
                'supplierOrder': {
                    'companyId': [
                        '3fa85f64-5717-4562-b3fc-2c963f66afa6'
                    ],
                    'contact': {
                        'userIds': [
                            '3fa85f64-5717-4562-b3fc-2c963f66afa6'
                        ]
                    }
                },
                'status': {
                    'processStatus': [
                        'Issued'
                    ],
                    'logisticsStatus': [
                        'Open'
                    ]
                },
                'lastUpdatedSince': '2021-04-23T10:01:53.812Z'
            },
            'sort': [
                {
                'field': 'buyerOrder.erpIssueDateTime',
                'order': 'asc'
                }
            ],
            'offset': 0,
            'limit': 0
        }";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud search orders example.");
            
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
            await SearchOrders();

            async Task SearchOrders()
            {                
                var jsonContent = jsonContentWithSingleQuotes.Replace("'", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(orderSearchUrl, content);
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
