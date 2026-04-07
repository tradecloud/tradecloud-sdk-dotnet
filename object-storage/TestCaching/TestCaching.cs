using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class TestCaching
    {
        /// <summary>API root including <c>/v2</c> (e.g. accp or a feature host).</summary>
        const string baseUrl = "https://api.accp.tradecloud1.com/v2";

        const string authenticationUrl = baseUrl + "/authentication/";
        const string uploadDocumentUrl = baseUrl + "/object-storage/document";
        const string objectStorageDocumentUrl = baseUrl + "/object-storage/document/";

        /// <summary>
        /// Optional file used for upload dedup and presign tests (same bytes uploaded twice in test 1).
        /// Leave empty to generate small temp files instead.
        /// </summary>
        const string testFileOverride = "";

        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Tradecloud Object Storage Caching Test (TC-10937) ===");
            Console.WriteLine();

            var waitForExpiry = args.Contains("--wait-for-expiry");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("ERROR: Fill in username and password in TestCaching.cs before running.");
                return;
            }

            var customTestFile = ResolveTestFilePath(testFileOverride);
            if (customTestFile != null)
            {
                if (!File.Exists(customTestFile))
                {
                    Console.WriteLine($"ERROR: test file not found: {customTestFile}");
                    Console.WriteLine("       Set testFileOverride to \"\" for generated small files, or fix the path.");
                    return;
                }

                Console.WriteLine($"Using test file: {customTestFile}");
                Console.WriteLine();
            }

            using var httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, _) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            Console.WriteLine("Authenticated successfully.");
            Console.WriteLine();

            await TestUploadDedup(httpClient, customTestFile);
            Console.WriteLine();
            await TestPresignCache(httpClient, customTestFile, waitForExpiry);

            Console.WriteLine();
            Console.WriteLine("=== All tests completed ===");
        }

        static async Task TestUploadDedup(HttpClient httpClient, string customTestFile)
        {
            Console.WriteLine("--- Test 1: Upload Deduplication ---");
            Console.WriteLine("Uploading the same file content twice.");
            Console.WriteLine();

            string filePath;
            var deleteAfter = false;
            if (customTestFile != null)
            {
                filePath = customTestFile;
            }
            else
            {
                var content = "Tradecloud caching dedup test - static content for hash comparison";
                filePath = Path.Combine(Path.GetTempPath(), "tc-cache-test-dedup.txt");
                File.WriteAllText(filePath, content);
                deleteAfter = true;
            }

            try
            {
                var id1 = await UploadDocument(httpClient, filePath, "Upload #1");
                var id2 = await UploadDocument(httpClient, filePath, "Upload #2");

                Console.WriteLine();
                Console.WriteLine($"  Upload #1 documentId: {id1}");
                Console.WriteLine($"  Upload #2 documentId: {id2}");

                if (id1 == id2)
                    Console.WriteLine("  RESULT: CACHED - same documentId returned (upload was deduplicated)");
                else
                    Console.WriteLine("  RESULT: NOT CACHED - different documentIds (each upload created a new S3 object)");
            }
            finally
            {
                if (deleteAfter)
                    try { File.Delete(filePath); } catch { }
            }
        }

        static async Task TestPresignCache(HttpClient httpClient, string customTestFile, bool waitForExpiry)
        {
            Console.WriteLine("--- Test 2: Presigned Download URL Caching ---");

            Console.WriteLine("Uploading a file to get a documentId...");
            string filePath;
            var deleteAfter = false;
            if (customTestFile != null)
            {
                filePath = customTestFile;
            }
            else
            {
                filePath = Path.Combine(Path.GetTempPath(), $"tc-cache-test-presign-{Guid.NewGuid():N}.txt");
                File.WriteAllText(filePath, $"Tradecloud presign cache test - {DateTime.UtcNow:O}");
                deleteAfter = true;
            }

            string objectId;
            try
            {
                objectId = await UploadDocument(httpClient, filePath, "Setup");
            }
            finally
            {
                if (deleteAfter)
                    try { File.Delete(filePath); } catch { }
            }

            if (string.IsNullOrEmpty(objectId))
            {
                Console.WriteLine("  SKIP: Could not upload test file.");
                return;
            }

            Console.WriteLine($"  documentId: {objectId}");
            Console.WriteLine();

            var objectStorage = new ObjectStorage(httpClient, objectStorageDocumentUrl);

            Console.WriteLine("Requesting metadata (1st call)...");
            var meta1 = await objectStorage.GetDocumentMetadata(objectId);
            var url1 = meta1.downloadUrl;
            Console.WriteLine($"  downloadUrl #1: {Truncate(url1, 120)}");
            Console.WriteLine();

            Console.WriteLine("Requesting metadata (2nd call, immediately after)...");
            var meta2 = await objectStorage.GetDocumentMetadata(objectId);
            var url2 = meta2.downloadUrl;
            Console.WriteLine($"  downloadUrl #2: {Truncate(url2, 120)}");
            Console.WriteLine();

            if (url1 == url2)
                Console.WriteLine("  RESULT: CACHED - same presigned URL returned");
            else
                Console.WriteLine("  RESULT: NOT CACHED - different presigned URLs returned");

            if (!waitForExpiry)
            {
                Console.WriteLine();
                Console.WriteLine("--- Test 2b: Presigned URL Expiry After TTL ---");
                Console.WriteLine("  Skipped. Re-run with --wait-for-expiry to wait 16 min and verify TTL expiry.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--- Test 2b: Presigned URL Expiry After TTL ---");
            var waitMinutes = 16;
            Console.WriteLine($"  Waiting {waitMinutes} minutes for presign cache to expire (TTL=15m)...");
            for (int i = 1; i <= waitMinutes; i++)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Console.WriteLine($"    {i}/{waitMinutes} minutes elapsed...");
            }

            Console.WriteLine("  Requesting metadata (3rd call, after TTL)...");
            var meta3 = await objectStorage.GetDocumentMetadata(objectId);
            var url3 = meta3.downloadUrl;
            Console.WriteLine($"  downloadUrl #3: {Truncate(url3, 120)}");
            Console.WriteLine();

            if (url3 == url1)
                Console.WriteLine("  RESULT: STILL CACHED after TTL - cache did NOT expire (unexpected)");
            else
                Console.WriteLine("  RESULT: NEW URL after TTL - cache expired correctly");
        }

        static async Task<string> UploadDocument(HttpClient httpClient, string filePath, string label)
        {
            using var fileStream = File.OpenRead(filePath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.Add("Content-Type", "application/octet-stream");

            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(streamContent, "file", Path.GetFileName(filePath));

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(uploadDocumentUrl, multipartContent);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            var responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"  {label}: status={statusCode} elapsed={watch.ElapsedMilliseconds}ms");

            if (statusCode < 200 || statusCode >= 300)
            {
                Console.WriteLine($"  {label}: ERROR response={responseString}");
                return null;
            }

            try
            {
                var json = JObject.Parse(responseString);
                return json["id"]?.ToString();
            }
            catch
            {
                Console.WriteLine($"  {label}: Could not parse response: {responseString}");
                return null;
            }
        }

        static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <returns>Full path, or <c>null</c> when <paramref name="path"/> is empty (use generated files).</returns>
        static string ResolveTestFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }
    }
}
