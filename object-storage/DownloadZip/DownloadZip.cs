using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using TradecloudService.Models;

namespace Com.Tradecloud1.SDK.Client
{
    class DownloadZip
    {
        const bool useToken = true;

        // Base URL - change this to switch between environments
        // Development: https://branch.d.tradecloud1.com
        // Test: https://api.test.tradecloud1.com  
        // Acceptance: https://api.accp.tradecloud1.com  
        const string baseUrl = "https://tc-10397-download-zip-gcs-url.d.tradecloud1.com";

        // Authentication
        const string username = "agrifac-integration@tradecloud1.com"; // Fill in mandatory username
        const string password = "SecretSecret1"; // Fill in mandatory password

        // API URLs - constructed from baseUrl
        static readonly string authenticationUrl = $"{baseUrl}/v2/authentication/";
        static readonly string sendOrderUrl = $"{baseUrl}/v2/api-connector/order";
        static readonly string uploadDocumentUrl = $"{baseUrl}/v2/object-storage/document";
        static readonly string attachOrderDocumentsUrl = $"{baseUrl}/v2/api-connector/order/documents";
        static readonly string downloadZipUrl = $"{baseUrl}/v2/object-storage/documents/zip";

        // Company and supplier info
        const string companyId = "f56aa4ce-8ec8-5197-bc26-77716a58add7";
        const string supplierAccountNumber = "540830";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud 500MB scale test: 10 documents (10MB each) with 5x ZIP multiplication.");
            Console.WriteLine("📋 Test structure: 1 order with 9 lines, 1 header doc + 9 line docs (10MB each)");
            Console.WriteLine("🔄 ZIP strategy: Each document included 5x in ZIP request = 50 total entries");
            Console.WriteLine("💾 Performance: Uses cached 10MB template document (fast on subsequent runs)");
            Console.WriteLine("📊 Upload: ~100MB, ZIP Download: ~500MB (5x multiplication effect)");

            using (HttpClient httpClient = new HttpClient())
            {
                // Authenticate
                if (useToken)
                {
                    var authenticationClient = new Authentication(httpClient, authenticationUrl);
                    var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }
                else
                {
                    var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);
                }

                try
                {
                    // Step 1: Create and send order with 9 lines
                    Console.WriteLine("\n=== Step 1: Creating order with 9 lines ===");
                    var purchaseOrder = await CreateOrderWith9Lines();
                    var orderResponse = await SendOrder(httpClient, purchaseOrder);

                    // Step 2: Create sample documents
                    Console.WriteLine("\n=== Step 2: Creating sample documents ===");
                    var documentFiles = CreateSampleDocuments();

                    // Step 3: Upload documents
                    Console.WriteLine("\n=== Step 3: Uploading documents ===");
                    var uploadedDocuments = await UploadDocuments(httpClient, documentFiles);

                    // Step 4: Attach documents to order
                    Console.WriteLine("\n=== Step 4: Attaching documents to order ===");
                    await AttachDocumentsToOrder(httpClient, purchaseOrder.Order.PurchaseOrderNumber, uploadedDocuments);

                    // Step 5: Request ZIP download URL with 5x multiplication
                    Console.WriteLine("\n=== Step 5: Requesting ZIP download URL with 5x multiplication ===");
                    var baseObjectIds = uploadedDocuments.Select(d => d.ObjectId).ToList();
                    var multipliedObjectIds = new List<string>();

                    // Include each objectId 5 times to create 55 total entries
                    for (int i = 1; i <= 5; i++)
                    {
                        multipliedObjectIds.AddRange(baseObjectIds);
                    }

                    Console.WriteLine($"Creating ZIP with {baseObjectIds.Count} unique documents × 5 copies = {multipliedObjectIds.Count} total entries");
                    var downloadUrl = await RequestZipDownloadUrl(httpClient, multipliedObjectIds);

                    // Step 6: Download ZIP file using the URL
                    Console.WriteLine("\n=== Step 6: Downloading ZIP file from URL ===");
                    await DownloadZipFile(httpClient, downloadUrl, $"order-documents-{DateTime.Now:yyyyMMdd-HHmmss}.zip");

                    // Step 7: Test concurrent requests (rate limiting test)
                    Console.WriteLine("\n=== Step 7: Testing concurrent ZIP requests (rate limiting) ===");
                    await TestConcurrentZipRequests(httpClient, multipliedObjectIds);

                    Console.WriteLine("\n=== Process completed successfully! ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        static Task<TradecloudPurchaseOrderRequestModel> CreateOrderWith9Lines()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string purchaseOrderNumber = $"PO-ZIP-TEST-{timestamp}";

            // Create the base order
            var purchaseOrder = OrderModelFactory.CreateCompleteOrder(
                companyId: companyId,
                purchaseOrderNumber: purchaseOrderNumber,
                supplierAccountNumber: supplierAccountNumber,
                buyerEmail: "buyer@example.com",
                supplierEmail: "supplier@example.com",
                description: "Test Order with 9 lines and 10 documents total");

            // Add 9 order lines
            for (int i = 1; i <= 9; i++)
            {
                string position = (i * 10).ToString("D4"); // 0010, 0020, 0030, etc.

                OrderModelFactory.AddOrderLine(
                    purchaseOrder,
                    position: position,
                    itemNumber: $"ITEM-{i:D3}",
                    itemName: $"Test Item {i} - 10MB Document",
                    quantity: i * 2, // 2, 4, 6, 8, etc.
                    deliveryDate: DateTime.Now.AddDays(14 + i).ToString(OrderModelFactory.Defaults.DateFormat),
                    unitOfMeasureIso: "PCE",
                    price: 10.50m + i, // 11.50, 12.50, 13.50, etc.
                    currencyIso: "EUR"
                );
            }

            Console.WriteLine($"Created order {purchaseOrderNumber} with {purchaseOrder.Lines.Count} lines");
            return Task.FromResult(purchaseOrder);
        }

        static List<(string fileName, byte[] content, string description, bool isHeaderDocument, int lineNumber)> CreateSampleDocuments()
        {
            var documents = new List<(string fileName, byte[] content, string description, bool isHeaderDocument, int lineNumber)>();

            Console.WriteLine("Creating 10 documents total: 1 header document + 9 line documents (10MB each)");
            Console.WriteLine("Using cached document template for performance...");

            // Load pre-generated cached document content
            var cachedContent = GetCachedDocument();
            var contentSizeMB = cachedContent.Length / (1024.0 * 1024.0);
            Console.WriteLine($"Using cached document: {cachedContent.Length:N0} bytes ({contentSizeMB:F1} MB)");

            // Create 1 header document using cached content
            Console.WriteLine("Creating 1 header document...");
            var headerFileName = "order-header-10mb.txt";
            documents.Add((headerFileName, cachedContent, "Order Header Document - 10MB", true, 0));
            Console.WriteLine($"✓ Created header document using cached content");

            // Create 9 line documents (1 per line) using cached content
            Console.WriteLine("Creating 9 line documents (1 per line)...");

            for (int lineNum = 1; lineNum <= 9; lineNum++)
            {
                var fileName = $"line-{lineNum:D2}-specification-10mb.txt";
                documents.Add((fileName, cachedContent, $"Line {lineNum} Specification - 10MB", false, lineNum));
                Console.WriteLine($"  Created document for line {lineNum}/9");
            }

            var totalSize = documents.Sum(d => d.content.Length);

            Console.WriteLine($"\n✓ Successfully created {documents.Count} documents using cached template:");
            Console.WriteLine($"   - 1 header document (10MB)");
            Console.WriteLine($"   - 9 line documents (10MB each)");
            Console.WriteLine($"📊 Upload size: {totalSize:N0} bytes ({totalSize / (1024.0 * 1024.0):F0} MB)");
            Console.WriteLine($"🔄 ZIP strategy: Each document will be included 5x = 50 total entries");
            Console.WriteLine($"📥 Expected ZIP download: ~{(totalSize * 5) / (1024.0 * 1024.0):F0} MB");
            Console.WriteLine($"⚡ Performance: Instant creation using cached 10MB template");

            return documents;
        }

        static byte[] GetCachedDocument()
        {
            const string cacheFileName = "cached-document-10mb.bin";
            const int expectedSizeBytes = 10 * 1024 * 1024; // 10 MB

            try
            {
                if (!File.Exists(cacheFileName))
                {
                    Console.WriteLine($"❌ Cached document not found: {cacheFileName}");
                    Console.WriteLine("Please run the setup command to generate the cached document first!");
                    throw new FileNotFoundException($"Required cached document not found: {cacheFileName}");
                }

                var cachedContent = File.ReadAllBytes(cacheFileName);
                var sizeMB = cachedContent.Length / (1024.0 * 1024.0);

                Console.WriteLine($"✓ Loaded cached document: {cacheFileName} ({cachedContent.Length:N0} bytes, {sizeMB:F1} MB)");

                if (cachedContent.Length < expectedSizeBytes * 0.9) // Allow 10% tolerance
                {
                    Console.WriteLine($"⚠ Warning: Cached document is smaller than expected ({sizeMB:F1} MB vs 10.0 MB)");
                }

                return cachedContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading cached document: {ex.Message}");
                Console.WriteLine("Please ensure the cached document is properly generated using the setup command.");
                throw;
            }
        }

        static async Task<List<UploadedDocument>> UploadDocuments(HttpClient httpClient, List<(string fileName, byte[] content, string description, bool isHeaderDocument, int lineNumber)> documents)
        {
            var uploadedDocuments = new List<UploadedDocument>();

            foreach (var doc in documents)
            {
                try
                {
                    Console.WriteLine($"Uploading {doc.fileName}...");

                    var multipartContent = new MultipartFormDataContent();
                    var fileContent = new ByteArrayContent(doc.content);
                    fileContent.Headers.Add("Content-Type", "text/plain");
                    multipartContent.Add(fileContent, "file", doc.fileName);

                    var start = DateTime.Now;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.PostAsync(uploadDocumentUrl, multipartContent);
                    watch.Stop();

                    var statusCode = (int)response.StatusCode;
                    var responseString = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Upload {doc.fileName}: status={statusCode}, elapsed={watch.ElapsedMilliseconds}ms");

                    if (response.IsSuccessStatusCode)
                    {
                        var uploadResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
                        string objectId = uploadResponse.id;

                        uploadedDocuments.Add(new UploadedDocument
                        {
                            ObjectId = objectId,
                            FileName = doc.fileName,
                            Description = doc.description,
                            IsHeaderDocument = doc.isHeaderDocument,
                            LineNumber = doc.lineNumber
                        });

                        Console.WriteLine($"Successfully uploaded {doc.fileName} with objectId: {objectId}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to upload {doc.fileName}: {responseString}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading {doc.fileName}: {ex.Message}");
                }
            }

            Console.WriteLine($"Successfully uploaded {uploadedDocuments.Count} out of {documents.Count} documents");
            return uploadedDocuments;
        }

        static async Task<dynamic> SendOrder(HttpClient httpClient, TradecloudPurchaseOrderRequestModel purchaseOrder)
        {
            var json = JsonConvert.SerializeObject(purchaseOrder, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Sending order {purchaseOrder.Order.PurchaseOrderNumber}...");
            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(sendOrderUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            var responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"SendOrder: status={statusCode}, elapsed={watch.ElapsedMilliseconds}ms");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Order {purchaseOrder.Order.PurchaseOrderNumber} sent successfully");
                return JsonConvert.DeserializeObject<dynamic>(responseString);
            }
            else
            {
                Console.WriteLine($"Failed to send order: {responseString}");
                throw new Exception($"Failed to send order: {response.StatusCode} - {responseString}");
            }
        }

        static async Task AttachDocumentsToOrder(HttpClient httpClient, string purchaseOrderNumber, List<UploadedDocument> uploadedDocuments)
        {
            // Prepare documents attachment request
            var headerDocuments = uploadedDocuments.Where(d => d.IsHeaderDocument).ToList();
            var lineDocuments = uploadedDocuments.Where(d => !d.IsHeaderDocument).ToList();

            Console.WriteLine($"Preparing to attach {headerDocuments.Count} header documents and {lineDocuments.Count} line documents");

            // Group line documents by LineNumber
            var documentsByLine = lineDocuments.GroupBy(d => d.LineNumber).OrderBy(g => g.Key).ToList();

            var attachRequest = new
            {
                order = new
                {
                    purchaseOrderNumber = purchaseOrderNumber,
                    documents = headerDocuments.Select(d => new
                    {
                        code = Guid.NewGuid().ToString(),
                        revision = "1",
                        name = d.FileName,
                        objectId = d.ObjectId,
                        type = "General",
                        description = d.Description
                    }).ToArray()
                },
                lines = documentsByLine.Select(lineGroup => new
                {
                    position = (lineGroup.Key * 10).ToString("D4"), // 0010, 0020, 0030, etc.
                    documents = lineGroup.Select(doc => new
                    {
                        code = Guid.NewGuid().ToString(),
                        revision = "1",
                        name = doc.FileName,
                        objectId = doc.ObjectId,
                        type = "General",
                        description = doc.Description
                    }).ToArray()
                }).ToArray()
            };

            Console.WriteLine($"Organized documents: {headerDocuments.Count} header + {documentsByLine.Count} lines with multiple documents each");

            var json = JsonConvert.SerializeObject(attachRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Attaching {uploadedDocuments.Count} documents to order {purchaseOrderNumber}...");
            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(attachOrderDocumentsUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            var responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"AttachDocuments: status={statusCode}, elapsed={watch.ElapsedMilliseconds}ms");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully attached {uploadedDocuments.Count} documents to order {purchaseOrderNumber}");
            }
            else
            {
                Console.WriteLine($"Failed to attach documents: {responseString}");
                throw new Exception($"Failed to attach documents: {response.StatusCode} - {responseString}");
            }
        }

        static async Task<string> RequestZipDownloadUrl(HttpClient httpClient, List<string> objectIds)
        {
            var fileName = $"order-documents-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var zipRequest = new
            {
                objectIds = objectIds.ToArray(),
                filename = fileName
            };

            var json = JsonConvert.SerializeObject(zipRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Requesting ZIP download URL for {objectIds.Count} documents with filename: {fileName} containing {objectIds.Count} documents");
            var requestWatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(downloadZipUrl, content);
            requestWatch.Stop();

            var statusCode = (int)response.StatusCode;
            var requestTime = requestWatch.ElapsedMilliseconds;
            Console.WriteLine($"ZIP URL request: status={statusCode}, request time={requestTime}ms");

            if (!response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to get ZIP download URL: {responseString}");
                throw new Exception($"Failed to get ZIP download URL: {response.StatusCode} - {responseString}");
            }

            // Parse the response to get download URL
            var responseJson = await response.Content.ReadAsStringAsync();
            var zipResponse = JsonConvert.DeserializeObject<CreateZipDownloadResponse>(responseJson);

            Console.WriteLine($"Received download URL: {zipResponse.DownloadUrl}");

            return zipResponse.DownloadUrl;
        }

        static async Task DownloadZipFile(HttpClient httpClient, string downloadUrl, string fileName)
        {
            Console.WriteLine($"Downloading ZIP file from provided URL (will extract actual filename from response, fallback: {fileName})");
            var downloadWatch = System.Diagnostics.Stopwatch.StartNew();
            var downloadResponse = await httpClient.GetAsync(downloadUrl);
            downloadWatch.Stop();

            var downloadStatusCode = (int)downloadResponse.StatusCode;
            var downloadTime = downloadWatch.ElapsedMilliseconds;
            Console.WriteLine($"ZIP download: status={downloadStatusCode}, download time={downloadTime}ms");

            if (!downloadResponse.IsSuccessStatusCode)
            {
                var downloadResponseString = await downloadResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to download ZIP file: {downloadResponseString}");
                throw new Exception($"Failed to download ZIP file: {downloadResponse.StatusCode} - {downloadResponseString}");
            }

            var zipBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            var fileSizeBytes = zipBytes.Length;
            var fileSizeKB = fileSizeBytes / 1024.0;
            var fileSizeMB = fileSizeKB / 1024.0;

            // Calculate bandwidth metrics for the download
            var downloadTimeSeconds = downloadTime / 1000.0;
            var downloadBandwidthBps = downloadTimeSeconds > 0 ? fileSizeBytes / downloadTimeSeconds : 0;
            var downloadBandwidthMbps = downloadBandwidthBps * 8 / 1000000.0; // Convert to Mbps

            // Extract filename from Content-Disposition header or fall back to provided filename
            var actualFileName = GetFileNameFromResponse(downloadResponse, fileName);
            await File.WriteAllBytesAsync(actualFileName, zipBytes);

            Console.WriteLine($"Successfully downloaded ZIP file: {actualFileName}");
            Console.WriteLine($"File size: {fileSizeBytes:N0} bytes ({fileSizeKB:F2} KB, {fileSizeMB:F2} MB)");
            Console.WriteLine($"Download time: {downloadTime}ms");
            Console.WriteLine($"Download bandwidth: {downloadBandwidthBps:F0} bytes/sec ({downloadBandwidthMbps:F2} Mbps)");
        }

        static async Task TestConcurrentZipRequests(HttpClient httpClient, List<string> objectIds)
        {
            Console.WriteLine($"Starting 6 concurrent ZIP URL requests (API limit: 5 concurrent per user)...");

            // Create 6 concurrent request tasks (should hit the 5 concurrent limit)
            var requestTasks = new List<Task<(int requestNumber, int statusCode, long elapsedMs, string downloadUrl)>>();

            for (int i = 1; i <= 6; i++)
            {
                int requestNumber = i; // Capture for closure
                requestTasks.Add(RequestZipConcurrent(httpClient, objectIds, requestNumber));
            }

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Execute all requests concurrently
            var results = await Task.WhenAll(requestTasks);

            watch.Stop();

            Console.WriteLine($"All 6 concurrent requests completed in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine("\nResults:");

            int successCount = 0;
            int rateLimitedCount = 0;
            int otherErrorCount = 0;

            foreach (var result in results.OrderBy(r => r.requestNumber))
            {
                string status = result.statusCode switch
                {
                    200 => "✓ SUCCESS",
                    429 => "⚠ RATE LIMITED",
                    _ => "✗ ERROR"
                };

                Console.WriteLine($"Request {result.requestNumber:D2}: {result.statusCode} {status} - {result.elapsedMs}ms");

                switch (result.statusCode)
                {
                    case 200: successCount++; break;
                    case 429: rateLimitedCount++; break;
                    default: otherErrorCount++; break;
                }
            }

            Console.WriteLine($"\nSummary:");
            Console.WriteLine($"- Successful requests: {successCount}");
            Console.WriteLine($"- Rate limited (429): {rateLimitedCount}");
            Console.WriteLine($"- Other errors: {otherErrorCount}");

            // Calculate request time statistics for successful requests
            var successfulResults = results.Where(r => r.statusCode == 200 && r.elapsedMs > 0).ToList();
            if (successfulResults.Any())
            {
                var requestTimes = successfulResults.Select(r => r.elapsedMs).ToList();
                var avgTime = requestTimes.Average();
                var minTime = requestTimes.Min();
                var maxTime = requestTimes.Max();

                Console.WriteLine($"\nRequest Time Statistics (successful requests):");
                Console.WriteLine($"- Average: {avgTime:F0}ms");
                Console.WriteLine($"- Minimum: {minTime}ms");
                Console.WriteLine($"- Maximum: {maxTime}ms");

                // Check for potential performance issues (significant variation could indicate throttling)
                var timeVariation = (maxTime - minTime) / avgTime;
                if (timeVariation > 1.0)
                {
                    Console.WriteLine($"⚠ Significant request time variation detected ({timeVariation:P1}) - possible rate limiting or performance issues");
                }
                else
                {
                    Console.WriteLine($"✓ Consistent request performance ({timeVariation:P1} variation)");
                }
            }

            if (rateLimitedCount > 0)
            {
                Console.WriteLine($"\n✓ Rate limiting is working - got {rateLimitedCount} 429 responses");
                Console.WriteLine("✓ API correctly enforces 5 concurrent requests per user limit");
            }
            else
            {
                Console.WriteLine("\n⚠ No rate limiting detected - all requests succeeded");
                Console.WriteLine("⚠ Either rate limiting is not active or concurrent load was handled");
            }
        }

        static async Task<(int requestNumber, int statusCode, long elapsedMs, string downloadUrl)> RequestZipConcurrent(
            HttpClient httpClient, List<string> objectIds, int requestNumber)
        {
            try
            {
                var fileName = $"concurrent-test-{requestNumber:D2}-{DateTime.Now:HHmmss}";
                var zipRequest = new
                {
                    objectIds = objectIds.ToArray(),
                    filename = fileName
                };

                var json = JsonConvert.SerializeObject(zipRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var requestWatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(downloadZipUrl, content);
                requestWatch.Stop();

                var statusCode = (int)response.StatusCode;
                var elapsedMs = requestWatch.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    return (requestNumber, statusCode, elapsedMs, string.Empty);
                }

                // Parse the response to get download URL
                var responseJson = await response.Content.ReadAsStringAsync();
                var zipResponse = JsonConvert.DeserializeObject<CreateZipDownloadResponse>(responseJson);

                return (requestNumber, statusCode, elapsedMs, zipResponse.DownloadUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request {requestNumber} failed with exception: {ex.Message}");
                return (requestNumber, 0, 0, string.Empty); // 0 indicates exception
            }
        }

        static string GetFileNameFromResponse(HttpResponseMessage response, string fallbackFileName)
        {
            try
            {
                // Try to get filename from Content-Disposition header
                if (response.Content.Headers.ContentDisposition?.FileName != null)
                {
                    var fileName = response.Content.Headers.ContentDisposition.FileName;
                    // Remove quotes if present
                    fileName = fileName.Trim('"');

                    Console.WriteLine($"Extracted filename from Content-Disposition header: {fileName}");
                    return fileName;
                }

                // Try to get filename from Content-Disposition FileNameStar (RFC 5987)
                if (response.Content.Headers.ContentDisposition?.FileNameStar != null)
                {
                    var fileName = response.Content.Headers.ContentDisposition.FileNameStar;
                    Console.WriteLine($"Extracted filename from Content-Disposition FileNameStar: {fileName}");
                    return fileName;
                }

                // Try to extract from the raw Content-Disposition header value
                if (response.Content.Headers.TryGetValues("Content-Disposition", out var dispositionValues))
                {
                    var dispositionHeader = dispositionValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(dispositionHeader))
                    {
                        // Look for filename= or filename*= in the header
                        var fileNameMatch = System.Text.RegularExpressions.Regex.Match(
                            dispositionHeader,
                            @"filename[*]?=[""']?([^""';]+)[""']?",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        if (fileNameMatch.Success)
                        {
                            var fileName = fileNameMatch.Groups[1].Value;
                            Console.WriteLine($"Extracted filename from Content-Disposition regex: {fileName}");
                            return fileName;
                        }
                    }
                }

                Console.WriteLine($"No filename found in response headers, using fallback: {fallbackFileName}");
                return fallbackFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting filename from response: {ex.Message}, using fallback: {fallbackFileName}");
                return fallbackFileName;
            }
        }
    }

    // Helper classes
    public class UploadedDocument
    {
        public string ObjectId { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public bool IsHeaderDocument { get; set; }
        public int LineNumber { get; set; } // 0 for header documents, 1-10 for line documents
    }

    public class CreateZipDownloadResponse
    {
        public string DownloadUrl { get; set; }
    }
}
