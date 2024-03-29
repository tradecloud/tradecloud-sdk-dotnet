﻿using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Com.Tradecloud1.SDK.Client
{
    class SendOrder
    {   
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/sci-connector/specs.yaml#/sci-connector/sendOrderByBuyerRoute
        const string sendOrderUrl = "https://api.accp.tradecloud1.com/v2/sci-connector/order";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud send SCSN order using Isah SCI Connector example.");
            
            XmlDocument order = new XmlDocument();
            order.Load("minimal-order.xml");
            string orderXml = order.OuterXml;

            HttpClient httpClient = new HttpClient();
            await SendOrder();

            async Task SendOrder()
            {                
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);
                var content = new StringContent(orderXml, Encoding.UTF8, "text/xml");
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PutAsync(sendOrderUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrder start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SendOrder request body=" + orderXml); 
                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrder response body=" +  responseString); 
            }
        }
    }
}
