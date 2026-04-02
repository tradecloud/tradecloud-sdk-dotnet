using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class DeleteDocument
    {
        // Bearer access token (Authorization header). Obtain via authentication/login or your token flow.
        const string token = "";

        // Document id (UUID) to delete.
        const string documentId = "";

        // https://api.accp.tradecloud1.com/v2/object-storage/private/specs.yaml#/object-storage/deleteDocument
        const string baseUrl = "https://api.accp.tradecloud1.com/v2";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud delete document example.");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Error: set `token` at the top of DeleteDocument.cs.");
                return;
            }

            if (string.IsNullOrWhiteSpace(documentId))
            {
                Console.WriteLine("Error: set `documentId` at the top of DeleteDocument.cs.");
                return;
            }

            var deleteDocumentUrl = $"{baseUrl}/object-storage/document/{documentId}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            Console.WriteLine("Deleting document... please wait");
            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.DeleteAsync(deleteDocumentUrl);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine(
                "DeleteDocument start=" + start + " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode
                + " reason=" + response.ReasonPhrase);

            var responseString = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseString))
                Console.WriteLine("DeleteDocument response body=" + responseString);
        }
    }
}
