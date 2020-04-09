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
        const string username = "frankjan@tradecloud1.com";
        // Fill in mandatory password
        const string password = "SecretSecret1";

        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/getDocumentMetadata
        // Fill in manadatory objectId
        const string getDocumentMetadataUrl = "https://api.accp.tradecloud1.com/v2/object-storage/document/67aa8ece-5d41-496f-a94c-483e360b833b/metadata";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud get document metadata example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var token = await authenticationClient.Authenticate(username, password);
            await GetDocumentMetadataRequest(token);

            async Task GetDocumentMetadataRequest(string token)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await httpClient.GetAsync(getDocumentMetadataUrl);

                Console.WriteLine("GetDocumentMetadata StatusCode: " + (int)response.StatusCode);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("GetDocumentMetadata Content: " +  JValue.Parse(responseString).ToString(Formatting.Indented));   
            }
        }
    }
}
