using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    public class ObjectStorage
    {   
        HttpClient httpClient;
        string objectStorageDocumentUrl;

        public ObjectStorage(HttpClient httpClient, string objectStorageDocumentUrl)
        {
            this.httpClient = httpClient;
            this.objectStorageDocumentUrl = objectStorageDocumentUrl;
        }

        public async Task<DocumentMetadata> GetDocumentMetadata(string objectId)
        {                        
            var url = objectStorageDocumentUrl + objectId + "/metadata";

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();            
            var response = await httpClient.GetAsync(url);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine("GetDocumentMetadata start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);
            string responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine("GetDocumentMetadata response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
            return JsonConvert.DeserializeObject<DocumentMetadata>(responseString);
        }
    }
}
