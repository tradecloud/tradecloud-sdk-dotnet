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

            // Header document
            var headerContent = Encoding.UTF8.GetBytes($"ORDER HEADER DOCUMENT\nCreated: {DateTime.Now}\nThis is the main order document containing general terms and conditions.");
            documents.Add(("order-header.txt", headerContent, "Order Header Document", true));

            // Line documents (one per line) - with some duplicate file names to test ZIP handling
            for (int i = 1; i <= 10; i++)
            {
                var lineContent = Encoding.UTF8.GetBytes($"LINE {i:D2} DOCUMENT\nItem: ITEM-{i:D3}\nQuantity: {i * 2}\nSpecial instructions for line {i}\nTechnical specifications and delivery notes.");

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
            }

            Console.WriteLine($"Created {documents.Count} sample documents (1 header + 10 line documents)");
            Console.WriteLine("Duplicate file names created:");
            Console.WriteLine("- specification.txt (lines 1-3)");
            Console.WriteLine("- manual.pdf (lines 4-6)");
            Console.WriteLine("- drawing.dwg (lines 7-8)");
            Console.WriteLine("- Unique names (lines 9-10)");
            return documents;
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
            // Prepare ZIP download request - send plain array of objectIds
            var json = JsonConvert.SerializeObject(objectIds.ToArray());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Requesting ZIP download for {objectIds.Count} documents...");
            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var response = await httpClient.PostAsync(downloadZipUrl, content);
            watch.Stop();

            var statusCode = (int)response.StatusCode;
            Console.WriteLine($"DownloadZip request: status={statusCode}, elapsed={watch.ElapsedMilliseconds}ms");

            if (response.IsSuccessStatusCode)
            {
                // Save the ZIP file
                var zipBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"order-documents-{DateTime.Now:yyyyMMdd-HHmmss}.zip";

                await File.WriteAllBytesAsync(fileName, zipBytes);

                Console.WriteLine($"Successfully downloaded ZIP file: {fileName} ({zipBytes.Length} bytes)");
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
            Console.WriteLine($"Starting 11 concurrent ZIP download requests...");

            // Create 11 concurrent download tasks
            var downloadTasks = new List<Task<(int requestNumber, int statusCode, long elapsedMs, int contentLength)>>();

            for (int i = 1; i <= 11; i++)
            {
                int requestNumber = i; // Capture for closure
                downloadTasks.Add(DownloadZipConcurrent(httpClient, objectIds, requestNumber));
            }

            var start = DateTime.Now;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Execute all downloads concurrently
            var results = await Task.WhenAll(downloadTasks);

            watch.Stop();

            Console.WriteLine($"All 11 concurrent requests completed in {watch.ElapsedMilliseconds}ms");
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

                Console.WriteLine($"Request {result.requestNumber:D2}: {result.statusCode} {status} - {result.elapsedMs}ms - {result.contentLength} bytes");

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

            if (rateLimitedCount > 0)
            {
                Console.WriteLine($"✓ Rate limiting is working - got {rateLimitedCount} 429 responses");
            }
            else
            {
                Console.WriteLine("⚠ No rate limiting detected - all requests succeeded");
            }
        }

        static async Task<(int requestNumber, int statusCode, long elapsedMs, int contentLength)> DownloadZipConcurrent(
            HttpClient httpClient, List<string> objectIds, int requestNumber)
        {
            try
            {
                // Prepare ZIP download request - send plain array of objectIds
                var json = JsonConvert.SerializeObject(objectIds.ToArray());
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
