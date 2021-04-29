using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class UploadDocument
    {   
        const bool useToken = true;
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/uploadDocument
        const string uploadDocumentUrl = "https://api.accp.tradecloud1.com/v2/object-storage/document";
        // Fill in manadatory local path
        const string path = "test.pdf";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud upload document example.");
            
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
            await UploadDocumentRequest();

            async Task UploadDocumentRequest()
            {                
                FileStream fileStream = File.OpenRead(path);

                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.Add("Content-Type", "application/octet-stream");
                streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + Path.GetFileName(path) + "\"");
                
                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(streamContent, "file", Path.GetFileName(path));

                Console.WriteLine("Uploading document...please wait");
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(uploadDocumentUrl, multipartContent);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("SendOrder start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                if (statusCode == 400)
                     Console.WriteLine("SendOrder request body=" + streamContent); 
                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SendOrder response body=" +  responseString); 
            }
        }
    }
}
