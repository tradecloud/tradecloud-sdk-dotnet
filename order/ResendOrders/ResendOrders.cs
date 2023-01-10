using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ResendOrders
    {
        const string accessToken = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/buyerResendOrderRoute
        const string resendOrderUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{orderId}/buyer/resend";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud resend orders.");

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            string[] orderIds = System.IO.File.ReadAllLines(@"orderIds.txt");

            foreach (var orderId in orderIds)
            {
                await ResendOrder(orderId);
            }   
                
            async Task ResendOrder(string orderId)            
            {                
                var resendOrderUrl = HttpUtility.UrlPathEncode(resendOrderUrlTemplate.Replace("{orderId}", orderId));
                var jsonContent = "{}";
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(resendOrderUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("ResendOrder orderId=" + orderId + " start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode != 200)
                {
                    Console.WriteLine("ResendOrder response body=" +  responseString);
                }
            }
        }
    }
}