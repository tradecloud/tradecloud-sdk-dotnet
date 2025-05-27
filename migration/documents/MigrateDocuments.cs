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
    // 1. Find all active orders in Tradecloud One using `orderSearchUrl`
    // 2. Find the legacy order by tenantId (buyer companyId) and code (purchaseOrderNumber) using `legacyFindOrderUrlTemplate`
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
    class MigrateDocuments
    {
        const legacyUsername = "";
        const legacyPassword = "";
        const accessToken = "";

        // Fill all active orders in Tradecloud One
        const string orderSearchUrl = "https://api.accp.tradecloud1.com/v2/order-search/search";

        // Find the legacy order by tenantId (buyer companyId) and code (purchaseOrderNumber)
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getOrderByCode
        const string legacyFindOrderUrlTemplate = "https://accp.tradecloud.nl/api/v1/purchaseOrderLine/tenant/{tenantId}/code/{code}";

        // Get the document from the legacy order
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderDocument
        const string legacyGetOrderDocumentUrlTemplate = "https://accp.tradecloud.nl/api/v1/purchaseOrder/{purchaseOrderId}/document/{fileId}";

        // Get the document from the legacy order line
        // https://accp.tradecloud.nl/api/v1/docs#!/Purchase_order_API/getPurchaseOrderLineDocument
        const string legacyGetOrderLineDocumentUrlTemplate = "https://accp.tradecloud.nl/api/v1/purchaseOrderLine/{purchaseOrderLineId}/document/{fileId}";

        // Upload the document to Tradecloud One
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/uploadDocument
        const string uploadDocumentUrl = "https://api.accp.tradecloud1.com/v2/object-storage/uploadDocument";

        // Attach the document to the TC1 order header
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/attachOrderDocument
        const string attachOrderDocumentUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{id}/document";

        // Attach the document to the TC1 order line
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml#/order/attachOrderLineDocument
        const string attachOrderLineDocumentUrlTemplate = "https://api.accp.tradecloud1.com/v2/order/{id}/line/{position}/document";
    }
}