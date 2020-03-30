using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace object_storage_upload_document
{
    class Program
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string loginUrl = "https://api.accp.tradecloud1.com/v2/authentication/login";
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
            var token = await Authenticate();
            await UploadDocument(token);

            async Task<string> Authenticate()
            {
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );

                var response = await httpClient.GetAsync(loginUrl);
                var content = response.Content;

                Console.WriteLine("Authenticate StatusCode: " + (int)response.StatusCode);

                string responseString = await content.ReadAsStringAsync();
                Console.WriteLine("Authenticate Content: " + responseString);         

                var token = response.Headers.GetValues("Set-Authorization").FirstOrDefault();
                return token;
            }

            async Task UploadDocument(string token)
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
