using System;
using System.IO;
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
            Console.WriteLine("Tradecloud send SCSN order with embedded file using Isah SCI Connector example.");
            
            XmlDocument order = new XmlDocument();
            order.Load("full-order.xml");
            string orderXml = order.OuterXml;
    
            Byte[] binaryFile = File.ReadAllBytes("example.file"); // Generate by using `truncate -s 24m example.file` (GNU coreutils)
            String encodedFile = Convert.ToBase64String(binaryFile);
            
            string orderWithFileXml = orderXml.Replace("embedded-base64-encoded-binary-file", encodedFile);
            
            HttpClient httpClient = new HttpClient();
            await SendOrder();

            async Task SendOrder()
            {                
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);
                var content = new StringContent(orderWithFileXml, Encoding.UTF8, "text/xml");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PutAsync(sendOrderUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrderWithFile start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                Console.WriteLine("SendOrderWithFile request headers=" + content.Headers);                     
                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrderWithFile response body=" +  responseString + " headers=" + response.Headers); 
            }
        }
    }
}
