using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

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
    // 4. Check if each TC1 order line has documents
    //    4.1 If not, check if the legacy order line has documents
    //        4.1.1 If yes, download the legacy document using `legacyGetOrderLineDocumentUrlTemplate`
    //        4.1.2 upload the legacy document to Tradecloud One using `uploadDocumentUrl`
    //        4.1.3 attach the document to the TC1 order line using `attachOrderLineDocumentUrlTemplate`
    //
    // Hints
    // - legacy tenantId = Tradecloud One buyer companyId
    // - legacy code = Tradecloud One purchaseOrderNumber
    // - legacy rowId = Tradecloud One order line position
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
        const string accessToken = "";
        const string buyerCompanyId = "";

        // Fill all active orders in Tradecloud One
        const string orderSearchUrl = baseUrl + "/order-search/search";

        // Find the legacy order by tenantId (buyer companyId) and code (purchaseOrderNumber)
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getOrderByCode
        const string legacyFindOrderUrlTemplate = legacyBaseUrl + "/purchaseOrderLine/tenant/{tenantId}/code/{code}";

        // Get the document from the legacy order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderDocument
        const string legacyGetOrderDocumentUrlTemplate = legacyBaseUrl + "/purchaseOrder/{purchaseOrderId}/document/{fileId}";

        // Get the document from the legacy order line
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderLineDocument
        const string legacyGetOrderLineDocumentUrlTemplate = legacyBaseUrl + "/purchaseOrderLine/{purchaseOrderLineId}/document/{fileId}";

        // Upload the document to Tradecloud One
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/uploadDocument
        const string uploadDocumentUrl = baseUrl + "/object-storage/uploadDocument";

        // Attach the document to the TC1 order header
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/attachOrderDocument
        const string attachOrderDocumentUrlTemplate = baseUrl + "/order/{id}/document";

        // Attach the document to the TC1 order line
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/attachOrderLineDocument
        const string attachOrderLineDocumentUrlTemplate = baseUrl + "/order/{id}/line/{position}/document";
    }
}