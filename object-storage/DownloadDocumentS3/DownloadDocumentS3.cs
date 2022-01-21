using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class DownloadDocument
    {   
        const bool useToken = true;
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/
        const string objectStorageDocumentUrl = "https://api.accp.tradecloud1.com/v2/object-storage/document/";

           // Fill in manadatory objectId
        const string objectId = "<objectId>";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud download document example.");
            
            using (HttpClient httpClient = new HttpClient())
            {
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

                var objectStorage = new ObjectStorage(httpClient, objectStorageDocumentUrl);
                var meta = await objectStorage.GetDocumentMetadata(objectId);
                await DownloadDocumentRequest();

                async Task DownloadDocumentRequest()
                {                
                    using (HttpClient httpS3Client = new HttpClient())
                    {
                        Console.WriteLine("DownloadDocument ... please wait");
                        
                        var start = DateTime.Now;
                        var watch = System.Diagnostics.Stopwatch.StartNew();                        
                        byte[] fileBytes = await httpS3Client.GetByteArrayAsync(meta.downloadUrl);                        
                        watch.Stop();

                        File.WriteAllBytes(meta.filename, fileBytes);

                        Console.WriteLine("DownloadDocument start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms");
                    }
                }
            }
        }
    }
}
