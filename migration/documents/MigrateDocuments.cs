using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DotNetEnv;

namespace Com.Tradecloud1.SDK.Client
{
    // Migrate documents from legacy system to Tradecloud One
    // 1. Find all active orders for the buyer company in Tradecloud One using `orderSearchUrl`
    //    - active orders do not have process status Completed and do not have a logistics status Delivered
    // 2. Find the legacy order by tenantId and code using `legacyFindOrderUrlTemplate`
    // 3. Check if the TC1 order header has documents
    //    3.1 If not, check if the legacy order header has documents
    //        3.1.1 If yes, download the legacy document using `legacyGetOrderDocumentUrlTemplate`
    //        3.1.2 upload the legacy document to Tradecloud One using `uploadDocumentUrl`
    //        3.1.3 attach the document to the TC1 order header using `attachOrderDocumentUrlTemplate`
    // 4. Find each legacy order line by purchaseOrderLineId using `legacyFindOrderLineUrlTemplate`
    // 5. Check if each TC1 order line has documents
    //    5.1 If not, check if the legacy order line has documents
    //        5.1.1 If yes, download the legacy document using `legacyGetOrderLineDocumentUrlTemplate`
    //        5.1.2 upload the legacy document to Tradecloud One using `uploadDocumentUrl`
    //        5.1.3 attach the document to the TC1 order line using `attachOrderLineDocumentUrlTemplate`
    //
    // Hints
    // - legacy tenantId = Tradecloud One buyer companyId
    // - legacy code = Tradecloud One purchaseOrderNumber
    // - legacy row = Tradecloud One order line position
    //
    // Requirements
    // - log the progress of the migration
    // - log for each migrated document: 
    //   - the TC1 purchaseOrderNumber and position (in case of an order line) 
    //   - legacy order code and row (in case of an order line)
    //   - legacy file id, title, mimeType
    //   - TC1 object id

    class MigrateDocuments
    {
        const string legacyBaseUrl = "https://accp.tradecloud.nl/api/v1";
        const string baseUrl = "https://api.accp.tradecloud1.com/v2";

        // Configuration loaded from environment variables
        static string legacyUsername;
        static string legacyPassword;
        static string accessToken;
        static string refreshToken; // Add refresh token support
        static string buyerCompanyId;
        static bool dryRun;

        // Control concurrency - conservative for document operations
        const int maxConcurrency = 3;
        static readonly SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        static readonly SemaphoreSlim logSemaphore = new SemaphoreSlim(1, 1); // For thread-safe logging
        static readonly SemaphoreSlim tokenRefreshSemaphore = new SemaphoreSlim(1, 1); // For thread-safe token refresh

        static readonly HttpClient httpClient = new HttpClient();
        static readonly HttpClient legacyHttpClient = new HttpClient();

        // Authentication URLs
        const string authenticationUrl = "https://api.tradecloud1.com/v2/authentication/";

        // Fill all active orders in Tradecloud One
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order-search/private/specs.yaml#/order-search/searchRoute
        const string orderSearchUrl = baseUrl + "/order-search/search";

        // Get the order by id in Tradecloud One
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/specs.yaml#/order/getOrderByIdRoute
        const string getOrderByIdUrl = baseUrl + "/order/{id}";

        // Find the legacy order by tenantId (buyer companyId) and code (purchaseOrderNumber)
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getOrderByCode
        const string legacyFindOrderUrlTemplate = legacyBaseUrl + "/purchaseOrder/tenant/{tenantId}/code/{code}";

        // Find the legacy order line by purchaseOrderLineId
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/findOrderLine
        const string legacyFindOrderLineUrlTemplate = legacyBaseUrl + "/purchaseOrderLine/{purchaseOrderLineId}";

        // Get the document from the legacy order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderDocument
        const string legacyGetOrderDocumentUrlTemplate = legacyBaseUrl + "/purchaseOrder/{purchaseOrderId}/document/{fileId}";

        // Get the document from the legacy order line
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderLineDocument
        const string legacyGetOrderLineDocumentUrlTemplate = legacyBaseUrl + "/purchaseOrderLine/{purchaseOrderLineId}/document/{fileId}";

        // Upload the document to Tradecloud One
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/uploadDocument
        const string uploadDocumentUrl = baseUrl + "/object-storage/document";

        // Attach the document to the TC1 order header
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/attachOrderDocument
        const string attachOrderDocumentUrlTemplate = baseUrl + "/order/{id}/document";

        // Attach the document to the TC1 order line
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/attachOrderLineDocument
        const string attachOrderLineDocumentUrlTemplate = baseUrl + "/order/{id}/line/{position}/document";

        // Progress tracking
        static int totalOrders = 0;
        static int processedOrders = 0;
        static int totalDocumentsMigrated = 0;
        static readonly object progressLock = new object();
        static DateTime migrationStartTime;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud document migration example");

            // Load environment variables from .env file
            Env.Load();

            // Load configuration from environment variables
            legacyUsername = Environment.GetEnvironmentVariable("LEGACY_USERNAME");
            legacyPassword = Environment.GetEnvironmentVariable("LEGACY_PASSWORD");
            accessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN");
            refreshToken = Environment.GetEnvironmentVariable("REFRESH_TOKEN"); // Add refresh token support
            buyerCompanyId = Environment.GetEnvironmentVariable("BUYER_COMPANY_ID");

            // Parse DRY_RUN as boolean, default to true for safety
            var raw = Environment.GetEnvironmentVariable("DRY_RUN");
            dryRun = string.IsNullOrEmpty(raw) || raw.Equals("true", StringComparison.OrdinalIgnoreCase);

            // Validate required configuration
            if (string.IsNullOrEmpty(legacyUsername) ||
                string.IsNullOrEmpty(legacyPassword) ||
                string.IsNullOrEmpty(accessToken) ||
                string.IsNullOrEmpty(buyerCompanyId))
            {
                Console.WriteLine("Error: Missing required environment variables. Please check your .env file.");
                Console.WriteLine("Required variables: LEGACY_USERNAME, LEGACY_PASSWORD, ACCESS_TOKEN, BUYER_COMPANY_ID");
                Console.WriteLine("Optional variables: REFRESH_TOKEN (recommended for long-running migrations)");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"Running in {(dryRun ? "DRY RUN" : "LIVE")} mode");
            Console.WriteLine($"Buyer company ID: {buyerCompanyId}");
            Console.WriteLine($"Max concurrency: {maxConcurrency}");
            Console.WriteLine($"Token refresh: {(string.IsNullOrEmpty(refreshToken) ? "Not configured" : "Available")}");

            // Setup authentication for Tradecloud One API using access token
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Setup authentication for legacy API
            var legacyCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(legacyUsername + ":" + legacyPassword));
            legacyHttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", legacyCredentials);

            await RunDocumentMigration();
        }

        static async Task RunDocumentMigration()
        {
            var logPath = $"{DateTime.Now:yyyyMMdd-HHmmss}-migrate-documents.log";

            using (var log = new StreamWriter(logPath, append: true))
            {
                migrationStartTime = DateTime.Now;
                await Log(log, "Starting document migration");
                await Log(log, $"Mode: {(dryRun ? "DRY RUN - No uploads/attachments will be performed" : "LIVE - Documents will be uploaded and attached")}");
                await Log(log, $"Buyer companyId={buyerCompanyId}");
                await Log(log, $"Max concurrency: {maxConcurrency}");

                try
                {
                    // Step 1: Get all active orders from Tradecloud One
                    Console.WriteLine("Loading active orders...");
                    await Log(log, "Loading active orders from Tradecloud One");

                    var activeOrders = await GetActiveOrders(log);
                    totalOrders = activeOrders.Count;

                    await Log(log, $"Found {activeOrders.Count} active orders to process");
                    Console.WriteLine($"Found {activeOrders.Count} active orders to process");

                    if (activeOrders.Count == 0)
                    {
                        await Log(log, "No active orders found. Migration completed.");
                        Console.WriteLine("No active orders found. Migration completed.");
                        return;
                    }

                    // Initial progress log
                    await LogProgress(log, "Starting concurrent processing");

                    // Process orders with controlled concurrency
                    var startTime = DateTime.Now;
                    var tasks = activeOrders.Select((order, index) =>
                        ProcessOrderWithSemaphore(order, log, index + 1));

                    var results = await Task.WhenAll(tasks);
                    var endTime = DateTime.Now;

                    var successCount = results.Count(r => r.success);
                    var totalMigrated = results.Sum(r => r.migratedDocuments);
                    var failureCount = results.Length - successCount;
                    var totalTime = endTime - startTime;

                    // Final summary
                    var summaryMessage = $"Migration completed in {totalTime:hh\\:mm\\:ss}. " +
                                       $"Processed {successCount}/{activeOrders.Count} orders successfully, " +
                                       $"{(dryRun ? "would migrate" : "migrated")} {totalMigrated} documents, " +
                                       $"{failureCount} failures";

                    await Log(log, summaryMessage);
                    Console.WriteLine(summaryMessage);
                    Console.WriteLine($"Detailed log: {logPath}");
                }
                catch (Exception ex)
                {
                    await Log(log, $"Migration failed with error: {ex.Message}");
                    Console.WriteLine($"Migration failed: {ex.Message}");
                }
            }
        }

        static async Task<(bool success, int migratedDocuments)> ProcessOrderWithSemaphore(JObject order, StreamWriter log, int orderNumber)
        {
            await semaphore.WaitAsync();
            try
            {
                var purchaseOrderNumber = order["buyerOrder"]["purchaseOrderNumber"]?.ToString();
                await Log(log, $"[{orderNumber}/{totalOrders}] Starting order: {purchaseOrderNumber}");

                var result = await ProcessOrder(order, log);

                await CompleteOrder(log, purchaseOrderNumber, result.migratedDocuments);
                await Log(log, $"[{orderNumber}/{totalOrders}] Completed order: {purchaseOrderNumber}, migrated {result.migratedDocuments} documents");

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        static async Task<(bool success, int migratedDocuments)> ProcessOrder(JObject order, StreamWriter log)
        {
            try
            {
                var orderId = order["id"]?.ToString();
                var purchaseOrderNumber = order["buyerOrder"]["purchaseOrderNumber"]?.ToString();

                await Log(log, $"Processing order: purchaseOrderNumber={purchaseOrderNumber}");

                // Step 2: Find corresponding legacy order
                var legacyOrder = await GetLegacyOrder(buyerCompanyId, purchaseOrderNumber, log);
                if (legacyOrder == null)
                {
                    await Log(log, $"Legacy order not found for: purchaseOrderNumber={purchaseOrderNumber}");
                    return (false, 0);
                }

                // Step 3: Migrate order header documents
                var headerDocs = await MigrateOrderHeaderDocuments(order, legacyOrder, log);

                // Step 4: Migrate order line documents
                var lineDocs = await MigrateOrderLineDocuments(order, legacyOrder, log);

                var totalDocs = headerDocs + lineDocs;
                await Log(log, $"Completed order: purchaseOrderNumber={purchaseOrderNumber}, migrated {totalDocs} documents");

                return (true, totalDocs);
            }
            catch (Exception ex)
            {
                await Log(log, $"Error processing order: purchaseOrderNumber={order["buyerOrder"]["purchaseOrderNumber"]}: {ex.Message}");
                return (false, 0);
            }
        }

        // Token refresh helper
        static async Task<bool> RefreshTokenIfNeeded(HttpResponseMessage response, StreamWriter log)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(refreshToken))
            {
                await tokenRefreshSemaphore.WaitAsync();
                try
                {
                    // Double-check pattern to avoid multiple refreshes
                    var staleToken = response.RequestMessage.Headers.Authorization?.Parameter;
                    if (httpClient.DefaultRequestHeaders.Authorization?.Parameter != staleToken)
                    {
                        // Token was already refreshed by another thread
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        return true;
                    }
                    else
                    {
                        try
                        {
                            // Create authentication client and refresh tokens
                            var authClient = new Authentication(httpClient, authenticationUrl);
                            (accessToken, refreshToken) = await authClient.Refresh(refreshToken);
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                            await Log(log, "Successfully refreshed access token");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            await Log(log, $"Failed to refresh token: {ex.Message}");
                            return false;
                        }
                    }
                }
                finally
                {
                    tokenRefreshSemaphore.Release();
                }
            }
            return false;
        }

        static async Task<List<JObject>> GetActiveOrders(StreamWriter log)
        {
            var orders = new List<JObject>();
            int offset = 0;
            const int pageSize = 100;
            bool hasMore = true;

            while (hasMore)
            {
                var searchRequest = new
                {
                    filters = new
                    {
                        buyerOrder = new
                        {
                            companyId = new[] { buyerCompanyId }
                        },
                        // Active orders: process status = Issued, InProgress, Confirmed, Rejected
                        // logistics status = Open, Produced, ReadyToShip, Shipped
                        status = new
                        {
                            processStatus = new[] { "Issued", "InProgress", "Confirmed", "Rejected" },
                            logisticsStatus = new[] { "Open", "Produced", "ReadyToShip", "Shipped" }
                        }
                    },
                    sort = new[]
                    {
                        new { field = "buyerOrder.purchaseOrderNumber", order = "asc" }
                    },
                    offset = offset,
                    limit = pageSize
                };

                var json = JsonConvert.SerializeObject(searchRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(orderSearchUrl, content);

                // Handle token refresh
                if (await RefreshTokenIfNeeded(response, log))
                {
                    // Create fresh content for retry since HttpContent is single-use
                    using var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(orderSearchUrl, retryContent);
                }

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await Log(log, $"Failed to get orders: response.StatusCode={response.StatusCode}, responseString={responseString}");
                    break;
                }

                var responseObj = JObject.Parse(responseString);
                var data = responseObj["data"] as JArray;

                if (data != null && data.Count > 0)
                {
                    foreach (JObject order in data)
                    {
                        orders.Add(order);
                    }
                    offset += pageSize;
                    hasMore = data.Count == pageSize;
                }
                else
                {
                    hasMore = false;
                }
            }

            return orders;
        }

        static async Task<JObject> GetLegacyOrder(string tenantId, string code, StreamWriter log)
        {
            var url = legacyFindOrderUrlTemplate
                .Replace("{tenantId}", tenantId)
                .Replace("{code}", code);

            try
            {
                var response = await legacyHttpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await Log(log, $"Failed to get legacy order: tenantId={tenantId}, code={code}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error getting legacy order: tenantId={tenantId}, code={code}, error={ex.Message}");
                return null;
            }
        }

        static async Task<JObject> GetLegacyOrderLine(string purchaseOrderLineId, StreamWriter log)
        {
            var url = legacyFindOrderLineUrlTemplate
                .Replace("{purchaseOrderLineId}", purchaseOrderLineId);

            try
            {
                var response = await legacyHttpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await Log(log, $"Failed to get legacy order line: purchaseOrderLineId={purchaseOrderLineId}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error getting legacy order line: purchaseOrderLineId={purchaseOrderLineId}, error={ex.Message}");
                return null;
            }
        }

        static async Task<JObject> GetOrderById(string orderId, StreamWriter log)
        {
            var url = getOrderByIdUrl.Replace("{id}", orderId);

            try
            {
                var response = await httpClient.GetAsync(url);

                // Handle token refresh
                if (await RefreshTokenIfNeeded(response, log))
                {
                    // Retry with refreshed token
                    response = await httpClient.GetAsync(url);
                }

                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await Log(log, $"Failed to get order by ID: orderId={orderId}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error getting order by ID: orderId={orderId}, error={ex.Message}");
                return null;
            }
        }

        static bool IsDocumentAlreadyMigrated(JObject legacyDoc, JArray tc1Documents, StreamWriter log)
        {
            //log.WriteLineAsync($"Checking if document is already migrated: legacyDoc={legacyDoc}, tc1Documents={tc1Documents}");
            if (tc1Documents == null || tc1Documents.Count == 0) return false;

            var legacyTitle = legacyDoc["title"]?.ToString();
            if (string.IsNullOrEmpty(legacyTitle)) return false;

            foreach (JObject tc1Doc in tc1Documents)
            {
                var tc1Name = tc1Doc["name"]?.ToString();

                // Check if this document was already migrated by comparing filenames
                if (tc1Name == legacyTitle)
                {
                    return true;
                }
            }

            return false;
        }

        static async Task<int> MigrateOrderHeaderDocuments(JObject tc1Order, JObject legacyOrder, StreamWriter log)
        {
            var migratedCount = 0;
            var orderId = tc1Order["id"]?.ToString();
            var purchaseOrderNumber = tc1Order["buyerOrder"]["purchaseOrderNumber"]?.ToString();

            // Get detailed order information to check existing documents
            var detailedOrder = await GetOrderById(orderId, log);
            if (detailedOrder == null)
            {
                await Log(log, $"Failed to get detailed order information: purchaseOrderNumber={purchaseOrderNumber}");
                return 0;
            }

            // Get existing TC1 documents from detailed order
            var tc1Documents = detailedOrder["buyerOrder"]["documents"] as JArray;

            // Check legacy order for documents
            var legacyDocuments = legacyOrder["documents"] as JArray;
            if (legacyDocuments == null || legacyDocuments.Count == 0)
            {
                await Log(log, $"Legacy order header has no documents: purchaseOrderNumber={purchaseOrderNumber}");
                return 0;
            }

            var legacyOrderId = legacyOrder["id"]?.ToString();

            foreach (JObject legacyDoc in legacyDocuments)
            {
                try
                {
                    var fileId = legacyDoc["id"]?.ToString();
                    var title = legacyDoc["title"]?.ToString();
                    var fileName = legacyDoc["filename"]?.ToString();
                    var mimeType = legacyDoc["mimeType"]?.ToString();

                    // Check if this document is already migrated
                    if (IsDocumentAlreadyMigrated(legacyDoc, tc1Documents, log))
                    {
                        await Log(log, $"Document already migrated, skipping: purchaseOrderNumber={purchaseOrderNumber}, title={title}, filename={fileName}");
                        continue;
                    }

                    // Download legacy document (always do this to verify access)
                    var documentBytes = await DownloadLegacyDocument(legacyOrderId, fileId, true, log);
                    if (documentBytes == null) continue;

                    string objectId = null;
                    bool attached = false;

                    if (dryRun)
                    {
                        // Dry run: simulate the upload and attach operations
                        objectId = $"dry-run-object-{Guid.NewGuid()}";
                        attached = true;
                        await Log(log, $"[DRY RUN] Would upload document: title={title}, documentBytes.Length={documentBytes.Length} bytes and attach to purchaseOrderNumber={purchaseOrderNumber}");
                    }
                    else
                    {
                        // Upload to Tradecloud One
                        objectId = await UploadToTradecloudOne(documentBytes, fileName, mimeType, log);
                        if (string.IsNullOrEmpty(objectId)) continue;

                        // Attach to order
                        attached = await AttachDocumentToOrder(orderId, purchaseOrderNumber, objectId, title, log);
                    }

                    if (attached)
                    {
                        migratedCount++;
                        await Log(log, $"{(dryRun ? "[DRY RUN] Would migrate" : "Migrated")} header document: purchaseOrderNumber={purchaseOrderNumber}, legacy_fileId={fileId}, title={title}, mime={mimeType}, tc1_object={objectId}");
                    }
                }
                catch (Exception ex)
                {
                    await Log(log, $"Error migrating header document: legacyDocId={legacyDoc["id"]}, error={ex.Message}");
                }
            }

            return migratedCount;
        }

        static async Task<int> MigrateOrderLineDocuments(JObject tc1Order, JObject legacyOrder, StreamWriter log)
        {
            var migratedCount = 0;
            var orderId = tc1Order["id"]?.ToString();
            var purchaseOrderNumber = tc1Order["buyerOrder"]["purchaseOrderNumber"]?.ToString();

            // Get detailed order information to check existing documents
            var detailedOrder = await GetOrderById(orderId, log);
            if (detailedOrder == null)
            {
                await Log(log, $"Failed to get detailed order information for lines: purchaseOrderNumber={purchaseOrderNumber}");
                return 0;
            }

            var tc1Lines = detailedOrder["lines"] as JArray;
            var legacyLines = legacyOrder["lines"] as JArray;

            if (tc1Lines == null || legacyLines == null) return 0;

            foreach (JObject tc1Line in tc1Lines)
            {
                try
                {
                    var position = tc1Line["buyerLine"]?["position"]?.ToString();

                    if (string.IsNullOrEmpty(position))
                    {
                        await Log(log, $"TC1 order line has empty position, skipping line");
                        continue;
                    }

                    // Get existing TC1 line documents from detailed order
                    var tc1LineDocuments = tc1Line["buyerLine"]["documents"] as JArray;

                    // Find corresponding legacy line by row (equivalent to TC1 position)
                    // TC1 positions are zero-padded (e.g., "00010"), legacy rows are not (e.g., "10")
                    var legacyOrderLine = legacyLines.FirstOrDefault(l =>
                        l["row"]?.ToString() == position?.TrimStart('0')) as JObject;

                    if (legacyOrderLine == null)
                    {
                        await Log(log, $"Legacy order line not found in order for purchaseOrderNumber={purchaseOrderNumber}, position={position}");
                        continue;
                    }

                    // Get the purchaseOrderLineId from the legacy order line
                    var purchaseOrderLineId = legacyOrderLine["id"]?.ToString();
                    if (string.IsNullOrEmpty(purchaseOrderLineId))
                    {
                        await Log(log, $"Legacy order line has no ID for purchaseOrderNumber={purchaseOrderNumber}, position={position}");
                        continue;
                    }

                    // Find legacy order line with documents using the ID
                    var legacyLineWithDocs = await GetLegacyOrderLine(purchaseOrderLineId, log);
                    if (legacyLineWithDocs == null)
                    {
                        await Log(log, $"Legacy order line details not found: purchaseOrderNumber={purchaseOrderNumber}, position={position}, purchaseOrderLineId={purchaseOrderLineId}");
                        continue;
                    }

                    var legacyLineDocuments = legacyLineWithDocs["documents"] as JArray;
                    if (legacyLineDocuments == null || legacyLineDocuments.Count == 0)
                    {
                        await Log(log, $"Legacy order line has no documents: purchaseOrderNumber={purchaseOrderNumber}, position={position}, purchaseOrderLineId={purchaseOrderLineId}");
                        continue;
                    }

                    var legacyLineId = legacyLineWithDocs["id"]?.ToString();

                    foreach (JObject legacyDoc in legacyLineDocuments)
                    {
                        try
                        {
                            var fileId = legacyDoc["id"]?.ToString();
                            var fileName = legacyDoc["filename"]?.ToString();
                            var title = legacyDoc["title"]?.ToString();
                            var mimeType = legacyDoc["mimeType"]?.ToString();

                            // Check if this document is already migrated
                            if (IsDocumentAlreadyMigrated(legacyDoc, tc1LineDocuments, log))
                            {
                                await Log(log, $"Document already migrated, skipping: purchaseOrderNumber={purchaseOrderNumber}, position={position}, title={title}, filename={fileName}");
                                continue;
                            }

                            // Download legacy document (always do this to verify access)
                            var documentBytes = await DownloadLegacyDocument(legacyLineId, fileId, false, log);
                            if (documentBytes == null) continue;

                            string objectId = null;
                            bool attached = false;

                            if (dryRun)
                            {
                                // Dry run: simulate the upload and attach operations
                                objectId = $"dry-run-object-{Guid.NewGuid()}";
                                attached = true;
                                await Log(log, $"[DRY RUN] Would upload document: title={title}, documentBytes.Length={documentBytes.Length} bytes, purchaseOrderNumber={purchaseOrderNumber}, position={position}");
                            }
                            else
                            {
                                // Upload to Tradecloud One
                                objectId = await UploadToTradecloudOne(documentBytes, fileName, mimeType, log);
                                if (string.IsNullOrEmpty(objectId)) continue;

                                // Attach to order line
                                attached = await AttachDocumentToOrderLine(orderId, purchaseOrderNumber, position, objectId, title, log);
                            }

                            if (attached)
                            {
                                migratedCount++;
                                await Log(log, $"{(dryRun ? "[DRY RUN] Would migrate" : "Migrated")} line document: purchaseOrderNumber={purchaseOrderNumber}, position={position}, legacy_fileId={fileId}, title={title}, mime={mimeType}, tc1_object={objectId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await Log(log, $"Error migrating line document: legacyDocId={legacyDoc["id"]}, error={ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Log(log, $"Error processing order line: position={tc1Line["position"]}, error={ex.Message}");
                }
            }

            return migratedCount;
        }

        static async Task<byte[]> DownloadLegacyDocument(string entityId, string fileId, bool isOrderDocument, StreamWriter log)
        {
            try
            {
                var url = isOrderDocument
                    ? legacyGetOrderDocumentUrlTemplate.Replace("{purchaseOrderId}", entityId).Replace("{fileId}", fileId)
                    : legacyGetOrderLineDocumentUrlTemplate.Replace("{purchaseOrderLineId}", entityId).Replace("{fileId}", fileId);

                var response = await legacyHttpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    await Log(log, $"Failed to download legacy document: fileId={fileId}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error downloading legacy document: fileId={fileId}, error={ex.Message}");
                return null;
            }
        }

        static async Task<string> UploadToTradecloudOne(byte[] documentBytes, string fileName, string mimeType, StreamWriter log)
        {
            try
            {
                await Log(log, $"Uploading document: fileName={fileName}, mimeType={mimeType}, documentBytes.Length={documentBytes.Length} bytes");
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(documentBytes);

                fileContent.Headers.Add("Content-Type", mimeType ?? "application/octet-stream");
                content.Add(fileContent, "file", fileName);

                var response = await httpClient.PostAsync(uploadDocumentUrl, content);

                // Handle token refresh
                if (await RefreshTokenIfNeeded(response, log))
                {
                    // Create fresh content for retry since HttpContent is single-use
                    using var retryContent = new MultipartFormDataContent();
                    using var retryFileContent = new ByteArrayContent(documentBytes);

                    retryFileContent.Headers.Add("Content-Type", mimeType ?? "application/octet-stream");
                    retryContent.Add(retryFileContent, "file", fileName);

                    response = await httpClient.PostAsync(uploadDocumentUrl, retryContent);
                }

                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseObj = JObject.Parse(responseString);
                    var objectId = responseObj["id"]?.ToString();
                    await Log(log, $"Uploaded document: fileName={fileName}, objectId={objectId}");
                    return objectId;
                }
                else
                {
                    await Log(log, $"Failed to upload document: fileName={fileName}, response.StatusCode={response.StatusCode}, responseString={responseString}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error uploading document: fileName={fileName}, error={ex.Message}");
                return null;
            }
        }

        static async Task<bool> AttachDocumentToOrder(string orderId, string purchaseOrderNumber, string objectId, string title, StreamWriter log)
        {
            try
            {
                var url = attachOrderDocumentUrlTemplate.Replace("{id}", orderId);
                var attachRequest = new
                {
                    documents = new[]
                    {
                        new
                        {
                            objectId = objectId,
                            name = title,
                            type = "General",
                            description = "Migrated from legacy system"
                        }
                    },
                    addedByCompanyId = buyerCompanyId
                };

                var json = JsonConvert.SerializeObject(attachRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);

                // Handle token refresh
                if (await RefreshTokenIfNeeded(response, log))
                {
                    // Create fresh content for retry since HttpContent is single-use
                    using var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(url, retryContent);
                }

                if (response.IsSuccessStatusCode)
                {
                    await Log(log, $"Attached document to order: purchaseOrderNumber={purchaseOrderNumber}, objectId={objectId}, title={title}");
                    return true;
                }
                else
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    await Log(log, $"Failed to attach document to order: purchaseOrderNumber={purchaseOrderNumber}, objectId={objectId}, response.StatusCode={response.StatusCode}, responseString={responseString}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error attaching document to order: purchaseOrderNumber={purchaseOrderNumber}, objectId={objectId}, error={ex.Message}");
                return false;
            }
        }

        static async Task<bool> AttachDocumentToOrderLine(string orderId, string purchaseOrderNumber, string position, string objectId, string title, StreamWriter log)
        {
            try
            {
                var url = attachOrderLineDocumentUrlTemplate.Replace("{id}", orderId).Replace("{position}", position);
                var attachRequest = new
                {
                    documents = new[]
                    {
                        new
                        {
                            objectId = objectId,
                            name = title,
                            type = "General",
                            description = "Migrated from legacy system"
                        }
                    },
                    addedByCompanyId = buyerCompanyId
                };

                var json = JsonConvert.SerializeObject(attachRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);

                // Handle token refresh
                if (await RefreshTokenIfNeeded(response, log))
                {
                    // Create fresh content for retry since HttpContent is single-use
                    using var retryContent = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(url, retryContent);
                }

                if (response.IsSuccessStatusCode)
                {
                    await Log(log, $"Attached document to order line: purchaseOrderNumber={purchaseOrderNumber}, position={position}, objectId={objectId}, title={title}");
                    return true;
                }
                else
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    await Log(log, $"Failed to attach document to order line: purchaseOrderNumber={purchaseOrderNumber}, position={position}, objectId={objectId}, response.StatusCode={response.StatusCode}, responseString={responseString}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await Log(log, $"Error attaching document to order line: purchaseOrderNumber={purchaseOrderNumber}, position={position}, objectId={objectId}, error={ex.Message}");
                return false;
            }
        }

        // Helper method to log with timestamp
        static async Task Log(StreamWriter log, string message)
        {
            // Thread-safe async logging
            await logSemaphore.WaitAsync();
            try
            {
                await log.WriteLineAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }
            finally
            {
                logSemaphore.Release();
            }
        }

        // Progress logging helper
        static async Task LogProgress(StreamWriter log, string additionalInfo = "")
        {
            lock (progressLock)
            {
                var elapsed = DateTime.Now - migrationStartTime;
                var progressPercent = totalOrders > 0 ? (processedOrders * 100.0 / totalOrders) : 0;
                var avgTimePerOrder = processedOrders > 0 ? elapsed.TotalSeconds / processedOrders : 0;
                var estimatedRemaining = avgTimePerOrder > 0 && totalOrders > processedOrders
                    ? TimeSpan.FromSeconds(avgTimePerOrder * (totalOrders - processedOrders))
                    : TimeSpan.Zero;

                var progressMessage = $"PROGRESS: {processedOrders}/{totalOrders} orders ({progressPercent:F1}%), " +
                                    $"{totalDocumentsMigrated} documents migrated, " +
                                    $"elapsed: {elapsed:hh\\:mm\\:ss}, " +
                                    $"avg: {avgTimePerOrder:F1}s/order";

                if (estimatedRemaining.TotalSeconds > 0)
                {
                    progressMessage += $", ETA: {estimatedRemaining:hh\\:mm\\:ss}";
                }

                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    progressMessage += $" - {additionalInfo}";
                }

                Console.WriteLine(progressMessage);
                _ = Task.Run(async () => await Log(log, progressMessage));
            }
        }

        // Update progress counters
        static void UpdateProgress(int documentsAdded = 0)
        {
            lock (progressLock)
            {
                if (documentsAdded > 0)
                {
                    totalDocumentsMigrated += documentsAdded;
                }
            }
        }

        // Mark order as completed
        static async Task CompleteOrder(StreamWriter log, string purchaseOrderNumber, int documentsMigrated)
        {
            lock (progressLock)
            {
                processedOrders++;
                totalDocumentsMigrated += documentsMigrated;
            }

            // Log every 10 orders or at specific milestones
            if (processedOrders % 10 == 0 || processedOrders == totalOrders)
            {
                await LogProgress(log, $"Completed: {purchaseOrderNumber}");
            }
        }
    }
}