using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        const string legacyUsername = "";
        const string legacyPassword = "";
        const string baseUrl = "https://api.accp.tradecloud1.com/v2";

        // Must be a buyer company admin, and not a support user, else object storage authorization will fail
        const string accessToken = "";
        const string buyerCompanyId = "";

        // Set to true to perform a dry run (logs everything but doesn't upload/attach documents)
        const bool dryRun = false;

        static readonly HttpClient httpClient = new HttpClient();
        static readonly HttpClient legacyHttpClient = new HttpClient();

        // Fill all active orders in Tradecloud One
        const string orderSearchUrl = baseUrl + "/order-search/search";

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

        static async Task Main(string[] args)
        {
            Console.WriteLine($"Tradecloud document migration example - {(dryRun ? "DRY RUN MODE" : "LIVE MODE")}");

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
                await log.WriteLineAsync($"Starting document migration at {DateTime.Now}");
                await log.WriteLineAsync($"Mode: {(dryRun ? "DRY RUN - No uploads/attachments will be performed" : "LIVE - Documents will be uploaded and attached")}");
                await log.WriteLineAsync($"Buyer companyId={buyerCompanyId}");

                try
                {
                    // Step 1: Get all active orders from Tradecloud One
                    var activeOrders = await GetActiveOrders(log);
                    await log.WriteLineAsync($"Found {activeOrders.Count} active orders to process");

                    int processedOrders = 0;
                    int migratedDocuments = 0;

                    foreach (var order in activeOrders)
                    {
                        try
                        {
                            var orderId = order["id"]?.ToString();
                            var purchaseOrderNumber = order["buyerOrder"]["purchaseOrderNumber"]?.ToString();

                            await log.WriteLineAsync($"Processing order: purchaseOrderNumber={purchaseOrderNumber}");

                            // Step 2: Find corresponding legacy order
                            var legacyOrder = await GetLegacyOrder(buyerCompanyId, purchaseOrderNumber, log);
                            if (legacyOrder == null)
                            {
                                await log.WriteLineAsync($"Legacy order not found for: purchaseOrderNumber={purchaseOrderNumber}");
                                continue;
                            }

                            // Step 3: Migrate order header documents
                            var headerDocs = await MigrateOrderHeaderDocuments(order, legacyOrder, log);
                            migratedDocuments += headerDocs;

                            // Step 4: Migrate order line documents
                            var lineDocs = await MigrateOrderLineDocuments(order, legacyOrder, log);
                            migratedDocuments += lineDocs;

                            processedOrders++;
                            await log.WriteLineAsync($"Completed order: purchaseOrderNumber={purchaseOrderNumber}, migrated {headerDocs + lineDocs} documents");
                        }
                        catch (Exception ex)
                        {
                            await log.WriteLineAsync($"Error processing order: purchaseOrderNumber={order["buyerOrder"]["purchaseOrderNumber"]}: {ex.Message}");
                        }
                    }

                    await log.WriteLineAsync($"Migration completed. Processed {processedOrders} orders, {(dryRun ? "would migrate" : "migrated")} {migratedDocuments} documents");
                }
                catch (Exception ex)
                {
                    await log.WriteLineAsync($"Migration failed with error: {ex.Message}");
                    Console.WriteLine($"Migration failed: {ex.Message}");
                }

                Console.WriteLine($"Migration completed. Check log file: {logPath}");
            }
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
                        // logistics status = Open, Produced, ReadyToShip, Shipped)
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
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await log.WriteLineAsync($"Failed to get orders: response.StatusCode={response.StatusCode}, responseString={responseString}");
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
                    await log.WriteLineAsync($"Failed to get legacy order: tenantId={tenantId}, code={code}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await log.WriteLineAsync($"Error getting legacy order: tenantId={tenantId}, code={code}, error={ex.Message}");
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
                    await log.WriteLineAsync($"Failed to get legacy order line: purchaseOrderLineId={purchaseOrderLineId}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await log.WriteLineAsync($"Error getting legacy order line: purchaseOrderLineId={purchaseOrderLineId}, error={ex.Message}");
                return null;
            }
        }

        static async Task<int> MigrateOrderHeaderDocuments(JObject tc1Order, JObject legacyOrder, StreamWriter log)
        {
            var migratedCount = 0;
            var orderId = tc1Order["id"]?.ToString();
            var purchaseOrderNumber = tc1Order["buyerOrder"]["purchaseOrderNumber"]?.ToString();

            // Check if TC1 order already has documents
            var tc1Documents = tc1Order["buyerOrder"]["documents"] as JArray;
            if (tc1Documents != null && tc1Documents.Count > 0)
            {
                await log.WriteLineAsync($"Order: purchaseOrderNumber={purchaseOrderNumber} already has {tc1Documents.Count} documents, skipping header migration");
                return 0;
            }

            // Check legacy order for documents
            var legacyDocuments = legacyOrder["documents"] as JArray;
            if (legacyDocuments == null || legacyDocuments.Count == 0)
            {
                await log.WriteLineAsync($"Legacy order header has no documents: purchaseOrderNumber={purchaseOrderNumber}");
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
                        await log.WriteLineAsync($"[DRY RUN] Would upload document: title={title}, documentBytes.Length={documentBytes.Length} bytes and attach to purchaseOrderNumber={purchaseOrderNumber}");
                    }
                    else
                    {
                        // Upload to Tradecloud One
                        objectId = await UploadToTradecloudOne(documentBytes, fileName, mimeType, log);
                        if (string.IsNullOrEmpty(objectId)) continue;

                        // Attach to order
                        attached = await AttachDocumentToOrder(orderId, objectId, title, log);
                    }

                    if (attached)
                    {
                        migratedCount++;
                        await log.WriteLineAsync($"{(dryRun ? "[DRY RUN] Would migrate" : "Migrated")} header document: purchaseOrderNumber={purchaseOrderNumber}, legacy_fileId={fileId}, title={title}, mime={mimeType}, tc1_object={objectId}");
                    }
                }
                catch (Exception ex)
                {
                    await log.WriteLineAsync($"Error migrating header document: legacyDocId={legacyDoc["id"]}, error={ex.Message}");
                }
            }

            return migratedCount;
        }

        static async Task<int> MigrateOrderLineDocuments(JObject tc1Order, JObject legacyOrder, StreamWriter log)
        {
            var migratedCount = 0;
            var orderId = tc1Order["id"]?.ToString();
            var purchaseOrderNumber = tc1Order["buyerOrder"]["purchaseOrderNumber"]?.ToString();

            var tc1Lines = tc1Order["lines"] as JArray;
            var legacyLines = legacyOrder["lines"] as JArray;

            if (tc1Lines == null || legacyLines == null) return 0;

            foreach (JObject tc1Line in tc1Lines)
            {
                try
                {
                    var position = tc1Line["buyerLine"]?["position"]?.ToString();

                    if (string.IsNullOrEmpty(position))
                    {
                        await log.WriteLineAsync($"TC1 order line has empty position, skipping line");
                        continue;
                    }

                    // Check if TC1 line already has documents
                    var tc1LineDocuments = tc1Line["buyerLine"]["documents"] as JArray;
                    if (tc1LineDocuments != null && tc1LineDocuments.Count > 0)
                    {
                        await log.WriteLineAsync($"Order line: purchaseOrderNumber={purchaseOrderNumber}, position={position} already has {tc1LineDocuments.Count} documents, skipping");
                        continue;
                    }

                    // Find corresponding legacy line by row (equivalent to TC1 position)
                    // TC1 positions are zero-padded (e.g., "00010"), legacy rows are not (e.g., "10")
                    var legacyOrderLine = legacyLines.FirstOrDefault(l =>
                        l["row"]?.ToString() == position?.TrimStart('0')) as JObject;

                    if (legacyOrderLine == null)
                    {
                        await log.WriteLineAsync($"Legacy order line not found in order for purchaseOrderNumber={purchaseOrderNumber}, position={position}");
                        continue;
                    }

                    // Get the purchaseOrderLineId from the legacy order line
                    var purchaseOrderLineId = legacyOrderLine["id"]?.ToString();
                    if (string.IsNullOrEmpty(purchaseOrderLineId))
                    {
                        await log.WriteLineAsync($"Legacy order line has no ID for purchaseOrderNumber={purchaseOrderNumber}, position={position}");
                        continue;
                    }

                    // Find legacy order line with documents using the ID
                    var legacyLineWithDocs = await GetLegacyOrderLine(purchaseOrderLineId, log);
                    if (legacyLineWithDocs == null)
                    {
                        await log.WriteLineAsync($"Legacy order line details not found: purchaseOrderNumber={purchaseOrderNumber}, position={position}, purchaseOrderLineId={purchaseOrderLineId}");
                        continue;
                    }

                    var legacyLineDocuments = legacyLineWithDocs["documents"] as JArray;
                    if (legacyLineDocuments == null || legacyLineDocuments.Count == 0)
                    {
                        await log.WriteLineAsync($"Legacy order line has no documents: purchaseOrderNumber={purchaseOrderNumber}, position={position}, purchaseOrderLineId={purchaseOrderLineId}");
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
                                await log.WriteLineAsync($"[DRY RUN] Would upload document: title={title}, documentBytes.Length={documentBytes.Length} bytes, purchaseOrderNumber={purchaseOrderNumber}, position={position}");
                            }
                            else
                            {
                                // Upload to Tradecloud One
                                objectId = await UploadToTradecloudOne(documentBytes, fileName, mimeType, log);
                                if (string.IsNullOrEmpty(objectId)) continue;

                                // Attach to order line
                                attached = await AttachDocumentToOrderLine(orderId, position, objectId, title, log);
                            }

                            if (attached)
                            {
                                migratedCount++;
                                await log.WriteLineAsync($"{(dryRun ? "[DRY RUN] Would migrate" : "Migrated")} line document: purchaseOrderNumber={purchaseOrderNumber}, position={position}, legacy_fileId={fileId}, title={title}, mime={mimeType}, tc1_object={objectId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await log.WriteLineAsync($"Error migrating line document: legacyDocId={legacyDoc["id"]}, error={ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await log.WriteLineAsync($"Error processing order line: position={tc1Line["position"]}, error={ex.Message}");
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
                    await log.WriteLineAsync($"Failed to download legacy document: fileId={fileId}, response.StatusCode={response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await log.WriteLineAsync($"Error downloading legacy document: fileId={fileId}, error={ex.Message}");
                return null;
            }
        }

        static async Task<string> UploadToTradecloudOne(byte[] documentBytes, string fileName, string mimeType, StreamWriter log)
        {
            try
            {
                await log.WriteLineAsync($"Uploading document: fileName={fileName}, mimeType={mimeType}, documentBytes.Length={documentBytes.Length} bytes");
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(documentBytes);

                fileContent.Headers.Add("Content-Type", mimeType ?? "application/octet-stream");
                content.Add(fileContent, "file", fileName);

                var response = await httpClient.PostAsync(uploadDocumentUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseObj = JObject.Parse(responseString);
                    var objectId = responseObj["id"]?.ToString();
                    await log.WriteLineAsync($"Uploaded document: fileName={fileName}, objectId={objectId}");
                    return objectId;
                }
                else
                {
                    await log.WriteLineAsync($"Failed to upload document: fileName={fileName}, response.StatusCode={response.StatusCode}, responseString={responseString}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await log.WriteLineAsync($"Error uploading document: fileName={fileName}, error={ex.Message}");
                return null;
            }
        }

        static async Task<bool> AttachDocumentToOrder(string orderId, string objectId, string title, StreamWriter log)
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

                if (response.IsSuccessStatusCode)
                {
                    await log.WriteLineAsync($"Attached document to order: orderId={orderId}, objectId={objectId}, title={title}");
                    return true;
                }
                else
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    await log.WriteLineAsync($"Failed to attach document to order: orderId={orderId}, objectId={objectId}, response.StatusCode={response.StatusCode}, responseString={responseString}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await log.WriteLineAsync($"Error attaching document to order: orderId={orderId}, objectId={objectId}, error={ex.Message}");
                return false;
            }
        }

        static async Task<bool> AttachDocumentToOrderLine(string orderId, string position, string objectId, string title, StreamWriter log)
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

                if (response.IsSuccessStatusCode)
                {
                    await log.WriteLineAsync($"Attached document to order line: orderId={orderId}, position={position}, objectId={objectId}, title={title}");
                    return true;
                }
                else
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    await log.WriteLineAsync($"Failed to attach document to order line: orderId={orderId}, position={position}, objectId={objectId}, response.StatusCode={response.StatusCode}, responseString={responseString}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await log.WriteLineAsync($"Error attaching document to order line: orderId={orderId}, position={position}, objectId={objectId}, error={ex.Message}");
                return false;
            }
        }
    }
}