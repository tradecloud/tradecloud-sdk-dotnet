using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class UploadDocument
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/login";
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
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var token = await authenticationClient.Authenticate(username, password);
            await UploadDocumentRequest(token);

            async Task UploadDocumentRequest(string token)
            {                
                FileStream fileStream = File.OpenRead(path);

                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.Add("Content-Type", "application/octet-stream");
                streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + Path.GetFileName(path) + "\"");
                
                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(streamContent, "file", Path.GetFileName(path));

                Console.WriteLine("Uploading document...please wait");
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await httpClient.PostAsync(uploadDocumentUrl, multipartContent);

                Console.WriteLine("UploadDocument StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("UploadDocument Content: " + responseString);      
            }
        }
    }
}
