using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class GetDocumentMetadata
    {   
        const bool useToken = true;
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/getDocumentMetadata
        // Fill in manadatory objectId
        const string getDocumentMetadataUrl = "https://api.accp.tradecloud1.com/v2/object-storage/document/<objectId>/metadata";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud get document metadata example.");
            
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
            await GetDocumentMetadataRequest();

            async Task GetDocumentMetadataRequest()
            {                        
                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(getDocumentMetadataUrl);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("GetDocumentMetadata start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GetDocumentMetadata response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
            }
        }
    }
}
