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
        const string baseUrl = "https://tc-10397-download-zip.d.tradecloud1.com";

        // Authentication
        const string username = ""; // Fill in mandatory username
        const string password = ""; // Fill in mandatory password

        // API URLs - constructed from baseUrl
        static readonly string authenticationUrl = $"{baseUrl}/v2/authentication/";
        static readonly string sendOrderUrl = $"{baseUrl}/v2/api-connector/order";
        static readonly string uploadDocumentUrl = $"{baseUrl}/v2/object-storage/document";
        static readonly string attachOrderDocumentsUrl = $"{baseUrl}/v2/api-connector/order/documents";
        static readonly string downloadZipUrl = $"{baseUrl}/v2/object-storage/documents/zip"; // New ZIP download endpoint

        // Company and supplier info
        const string companyId = "f56aa4ce-8ec8-5197-bc26-77716a58add7";
        const string supplierAccountNumber = "540830";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud comprehensive order with documents and ZIP download example.");

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
                    // Step 1: Create and send order with 10 lines
                    Console.WriteLine("\n=== Step 1: Creating order with 10 lines ===");
                    var purchaseOrder = await CreateOrderWith10Lines();
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

                    // Step 5: Download all documents as ZIP
                    Console.WriteLine("\n=== Step 5: Downloading documents as ZIP ===");
                    await DownloadDocumentsZip(httpClient, uploadedDocuments.Select(d => d.ObjectId).ToList());

                    // Step 6: Test concurrent downloads (rate limiting test)
                    Console.WriteLine("\n=== Step 6: Testing concurrent ZIP downloads (rate limiting) ===");
                    await TestConcurrentZipDownloads(httpClient, uploadedDocuments.Select(d => d.ObjectId).ToList());

                    Console.WriteLine("\n=== Process completed successfully! ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        static Task<TradecloudPurchaseOrderRequestModel> CreateOrderWith10Lines()
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
                description: "Test Order with 10 lines for ZIP download");

            // Add 10 order lines
            for (int i = 1; i <= 10; i++)
            {
                string position = (i * 10).ToString("D4"); // 0010, 0020, 0030, etc.

                OrderModelFactory.AddOrderLine(
                    purchaseOrder,
                    position: position,
                    itemNumber: $"ITEM-{i:D3}",
                    itemName: $"Test Item {i}",
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

        static List<(string fileName, byte[] content, string description, bool isHeaderDocument)> CreateSampleDocuments()
        {
            var documents = new List<(string fileName, byte[] content, string description, bool isHeaderDocument)>();

            Console.WriteLine("Creating large test documents to test bandwidth limiting...");

            // Header document - make it large (500 KB)
            var headerContent = CreateLargeDocument("ORDER HEADER DOCUMENT", 500 * 1024); // 500 KB
            documents.Add(("order-header.txt", headerContent, "Order Header Document", true));
            Console.WriteLine($"Created header document: {headerContent.Length:N0} bytes");

            // Line documents (one per line) - each 300-800 KB to test bandwidth limiting
            for (int i = 1; i <= 10; i++)
            {
                var baseSizeKB = 300 + (i * 50); // 350KB, 400KB, 450KB, etc. up to 800KB
                var lineContent = CreateLargeDocument($"LINE {i:D2} DOCUMENT", baseSizeKB * 1024);

                // Create duplicate file names for some documents to test ZIP suffix handling
                string fileName;
                if (i <= 3)
                {
                    // Lines 1-3: Use "specification.txt" (duplicate names)
                    fileName = "specification.txt";
                }
                else if (i <= 6)
                {
                    // Lines 4-6: Use "manual.pdf" (duplicate names)  
                    fileName = "manual.pdf";
                }
                else if (i <= 8)
                {
                    // Lines 7-8: Use "drawing.dwg" (duplicate names)
                    fileName = "drawing.dwg";
                }
                else
                {
                    // Lines 9-10: Unique names
                    fileName = $"line-{i:D2}-spec.txt";
                }

                documents.Add((fileName, lineContent, $"Line {i} Specifications", false));
                Console.WriteLine($"Created line {i} document: {lineContent.Length:N0} bytes");
            }

            var totalSize = documents.Sum(d => d.content.Length);
            Console.WriteLine($"Created {documents.Count} sample documents (1 header + 10 line documents)");
            Console.WriteLine($"Total size: {totalSize:N0} bytes ({totalSize / 1024.0:F1} KB, {totalSize / (1024.0 * 1024.0):F1} MB)");
            Console.WriteLine("Duplicate file names created:");
            Console.WriteLine("- specification.txt (lines 1-3)");
            Console.WriteLine("- manual.pdf (lines 4-6)");
            Console.WriteLine("- drawing.dwg (lines 7-8)");
            Console.WriteLine("- Unique names (lines 9-10)");
            Console.WriteLine("Large files should help test 1 MiB/s bandwidth limiting!");
            return documents;
        }

        static byte[] CreateLargeDocument(string baseText, int targetSizeBytes)
        {
            var content = new List<string>();
            content.Add($"{baseText}\nCreated: {DateTime.Now}\n");
            content.Add("=".PadRight(80, '=') + "\n");

            // Add structured content to reach target size
            var random = new Random(42); // Fixed seed for consistent results
            var currentSize = 0;
            var lineCount = 0;

            while (currentSize < targetSizeBytes)
            {
                lineCount++;
                var lineText = $"Line {lineCount:D6}: ";

                // Add different types of content
                switch (lineCount % 5)
                {
                    case 0:
                        lineText += $"Technical specification: Item weight {random.Next(1, 100)}kg, dimensions {random.Next(10, 200)}x{random.Next(10, 200)}x{random.Next(10, 200)}mm";
                        break;
                    case 1:
                        lineText += $"Quality control data: Batch {Guid.NewGuid()}, tested on {DateTime.Now.AddDays(-random.Next(1, 30)):yyyy-MM-dd}, result: PASS";
                        break;
                    case 2:
                        lineText += $"Manufacturing details: Process temperature {random.Next(200, 500)}°C, pressure {random.Next(1, 10)} bar, duration {random.Next(30, 180)} minutes";
                        break;
                    case 3:
                        lineText += $"Supply chain info: Supplier code SUP-{random.Next(1000, 9999)}, delivery date {DateTime.Now.AddDays(random.Next(1, 60)):yyyy-MM-dd}, priority level {random.Next(1, 5)}";
                        break;
                    case 4:
                        lineText += $"Compliance data: Certificate {Guid.NewGuid()}, valid until {DateTime.Now.AddYears(1):yyyy-MM-dd}, standard ISO-{random.Next(9000, 9999)}";
                        break;
                }

                // Add some random padding to vary line lengths
                var padding = new string('*', random.Next(0, 50));
                lineText += $" {padding}\n";

                content.Add(lineText);
                currentSize = Encoding.UTF8.GetByteCount(string.Join("", content));
            }

            content.Add("\n" + "=".PadRight(80, '=') + "\n");
            content.Add($"Document end. Total lines: {lineCount}, Final size: {currentSize:N0} bytes\n");

            return Encoding.UTF8.GetBytes(string.Join("", content));
        }

        static async Task<List<UploadedDocument>> UploadDocuments(HttpClient httpClient, List<(string fileName, byte[] content, string description, bool isHeaderDocument)> documents)
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
                            IsHeaderDocument = doc.isHeaderDocument
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
                lines = lineDocuments.Select((doc, index) => new
                {
                    position = ((index + 1) * 10).ToString("D4"), // 0010, 0020, 0030, etc.
                    documents = new[]
                    {
                        new
                        {
                            code = Guid.NewGuid().ToString(),
                            revision = "1",
                            name = doc.FileName,
                            objectId = doc.ObjectId,
                            type = "General",
                            description = doc.Description
                        }
                    }
                }).ToArray()
            };

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

        static async Task DownloadDocumentsZip(HttpClient httpClient, List<string> objectIds)
        {
            // Prepare ZIP download request with new API format
            var fileName = $"order-documents-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var zipRequest = new
            {
                objectIds = objectIds.ToArray(),
                filename = fileName
            };

            var json = JsonConvert.SerializeObject(zipRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Requesting ZIP download for {objectIds.Count} documents with filename: {fileName}");
            var start = DateTime.Now;
            var requestWatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(downloadZipUrl, content);
            var requestTime = requestWatch.ElapsedMilliseconds;

            var statusCode = (int)response.StatusCode;
            Console.WriteLine($"DownloadZip request: status={statusCode}, request time={requestTime}ms");

            if (response.IsSuccessStatusCode)
            {
                // Download and measure bandwidth
                var downloadWatch = System.Diagnostics.Stopwatch.StartNew();
                var zipBytes = await response.Content.ReadAsByteArrayAsync();
                downloadWatch.Stop();

                var totalTime = requestWatch.ElapsedMilliseconds;
                var downloadTime = downloadWatch.ElapsedMilliseconds;
                var fileSizeBytes = zipBytes.Length;
                var fileSizeKB = fileSizeBytes / 1024.0;
                var fileSizeMB = fileSizeKB / 1024.0;

                // Calculate bandwidth (bytes per second and MiB/s)
                var totalTimeSeconds = totalTime / 1000.0;
                var downloadTimeSeconds = downloadTime / 1000.0;
                var totalBandwidthBps = totalTimeSeconds > 0 ? fileSizeBytes / totalTimeSeconds : 0;
                var downloadBandwidthBps = downloadTimeSeconds > 0 ? fileSizeBytes / downloadTimeSeconds : 0;
                var totalBandwidthMiBps = totalBandwidthBps / (1024.0 * 1024.0); // Convert to MiB/s
                var downloadBandwidthMiBps = downloadBandwidthBps / (1024.0 * 1024.0); // Convert to MiB/s

                await File.WriteAllBytesAsync(fileName, zipBytes);

                Console.WriteLine($"Successfully downloaded ZIP file: {fileName}");
                Console.WriteLine($"File size: {fileSizeBytes:N0} bytes ({fileSizeKB:F2} KB, {fileSizeMB:F2} MB)");
                Console.WriteLine($"Total time: {totalTime}ms (request: {requestTime}ms, download: {downloadTime}ms)");
                Console.WriteLine($"Total bandwidth: {totalBandwidthBps:F0} bytes/sec ({totalBandwidthMiBps:F3} MiB/s)");
                Console.WriteLine($"Download bandwidth: {downloadBandwidthBps:F0} bytes/sec ({downloadBandwidthMiBps:F3} MiB/s)");
                Console.WriteLine($"ZIP file contains {objectIds.Count} documents");
            }
            else
            {
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to download ZIP: {responseString}");
                throw new Exception($"Failed to download ZIP: {response.StatusCode} - {responseString}");
            }
        }

        static async Task TestConcurrentZipDownloads(HttpClient httpClient, List<string> objectIds)
        {
            Console.WriteLine($"Starting 6 concurrent ZIP download requests (API limit: 5 concurrent per user)...");

            // Create 6 concurrent download tasks (should hit the 5 concurrent limit)
            var downloadTasks = new List<Task<(int requestNumber, int statusCode, long elapsedMs, int contentLength)>>();

            for (int i = 1; i <= 6; i++)
            {
                int requestNumber = i; // Capture for closure
                downloadTasks.Add(DownloadZipConcurrent(httpClient, objectIds, requestNumber));
            }

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Execute all downloads concurrently
            var results = await Task.WhenAll(downloadTasks);

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

                if (result.statusCode == 200 && result.contentLength > 0)
                {
                    var bandwidthBps = result.elapsedMs > 0 ? (result.contentLength * 1000.0) / result.elapsedMs : 0;
                    var bandwidthMiBps = bandwidthBps / (1024.0 * 1024.0);
                    Console.WriteLine($"Request {result.requestNumber:D2}: {result.statusCode} {status} - {result.elapsedMs}ms - {result.contentLength:N0} bytes - {bandwidthBps:F0} bytes/sec ({bandwidthMiBps:F3} MiB/s)");
                }
                else
                {
                    Console.WriteLine($"Request {result.requestNumber:D2}: {result.statusCode} {status} - {result.elapsedMs}ms - {result.contentLength} bytes");
                }

                switch (result.statusCode)
                {
                    case 200: successCount++; break;
                    case 429: rateLimitedCount++; break;
                    default: otherErrorCount++; break;
                }
            }

            Console.WriteLine($"\nSummary:");
            Console.WriteLine($"- Successful downloads: {successCount}");
            Console.WriteLine($"- Rate limited (429): {rateLimitedCount}");
            Console.WriteLine($"- Other errors: {otherErrorCount}");

            // Calculate bandwidth statistics for successful downloads
            var successfulResults = results.Where(r => r.statusCode == 200 && r.contentLength > 0 && r.elapsedMs > 0).ToList();
            if (successfulResults.Any())
            {
                var bandwidths = successfulResults.Select(r => (r.contentLength * 1000.0) / r.elapsedMs).ToList();
                var avgBandwidth = bandwidths.Average();
                var minBandwidth = bandwidths.Min();
                var maxBandwidth = bandwidths.Max();
                var avgBandwidthMiBps = avgBandwidth / (1024.0 * 1024.0);
                var minBandwidthMiBps = minBandwidth / (1024.0 * 1024.0);
                var maxBandwidthMiBps = maxBandwidth / (1024.0 * 1024.0);

                Console.WriteLine($"\nBandwidth Statistics (successful downloads):");
                Console.WriteLine($"- Average: {avgBandwidth:F0} bytes/sec ({avgBandwidthMiBps:F3} MiB/s)");
                Console.WriteLine($"- Minimum: {minBandwidth:F0} bytes/sec ({minBandwidthMiBps:F3} MiB/s)");
                Console.WriteLine($"- Maximum: {maxBandwidth:F0} bytes/sec ({maxBandwidthMiBps:F3} MiB/s)");

                // Check for potential bandwidth limiting (significant variation could indicate throttling)
                var bandwidthVariation = (maxBandwidth - minBandwidth) / avgBandwidth;
                if (bandwidthVariation > 0.5)
                {
                    Console.WriteLine($"⚠ Significant bandwidth variation detected ({bandwidthVariation:P1}) - possible bandwidth limiting");
                }
                else
                {
                    Console.WriteLine($"✓ Consistent bandwidth performance ({bandwidthVariation:P1} variation)");
                }
            }

            if (rateLimitedCount > 0)
            {
                Console.WriteLine($"\n✓ Rate limiting is working - got {rateLimitedCount} 429 responses");
                Console.WriteLine("✓ API correctly enforces 5 concurrent downloads per user limit");
            }
            else
            {
                Console.WriteLine("\n⚠ No rate limiting detected - all requests succeeded");
                Console.WriteLine("⚠ Either rate limiting is not active or concurrent load was handled");
            }
        }

        static async Task<(int requestNumber, int statusCode, long elapsedMs, int contentLength)> DownloadZipConcurrent(
            HttpClient httpClient, List<string> objectIds, int requestNumber)
        {
            try
            {
                // Prepare ZIP download request with new API format
                var fileName = $"concurrent-test-{requestNumber:D2}-{DateTime.Now:HHmmss}.zip";
                var zipRequest = new
                {
                    objectIds = objectIds.ToArray(),
                    filename = fileName
                };

                var json = JsonConvert.SerializeObject(zipRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(downloadZipUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                int contentLength = 0;

                if (response.IsSuccessStatusCode)
                {
                    var zipBytes = await response.Content.ReadAsByteArrayAsync();
                    contentLength = zipBytes.Length;
                }

                return (requestNumber, statusCode, watch.ElapsedMilliseconds, contentLength);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request {requestNumber} failed with exception: {ex.Message}");
                return (requestNumber, 0, 0, 0); // 0 indicates exception
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
    }
}
