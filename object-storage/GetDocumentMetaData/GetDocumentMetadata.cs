using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class GetDocumentMetadata
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/login";
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
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken)  = await authenticationClient.Authenticate(username, password);
            await GetDocumentMetadataRequest(accessToken);

            async Task GetDocumentMetadataRequest(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.GetAsync(getDocumentMetadataUrl);

                Console.WriteLine("GetDocumentMetadata StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GetDocumentMetadata Content: " +  JValue.Parse(responseString).ToString(Formatting.Indented));   
            }
        }
    }
}
