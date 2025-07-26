# Download Documents ZIP

This example demonstrates a comprehensive workflow for working with Tradecloud orders and documents:

1. **Create Order** - Creates an order with 10 lines 
2. **Upload Documents** - Uploads 11 documents (1 header document + 10 line documents)
3. **Attach Documents** - Attaches documents to the order (header document to order, line documents to respective lines)
4. **Download ZIP** - Downloads all documents as a single ZIP file using the new getDocumentsZip API
5. **Test Rate Limiting** - Tests concurrent ZIP downloads to verify 429 (Too Many Requests) rate limiting behavior

## Features

- Creates a complete purchase order with 10 order lines
- Generates sample documents for testing (text files with relevant content)
- Uploads documents to Tradecloud object storage
- Attaches documents to the appropriate order/lines
- Downloads all documents in a single ZIP file
- Tests concurrent downloads to verify rate limiting (429 responses)
- Comprehensive error handling and progress reporting

## Prerequisites

A Tradecloud user with appropriate permissions:

- `buyer` role for creating orders
- Document upload/download permissions
- Access to the object storage ZIP download API

## Configuration

In the source code, update the following constants:

```csharp
// Base URL - choose your environment
// const string baseUrl = "https://branch.d.tradecloud1.com"; // Development
// const string baseUrl = "https://api.test.tradecloud1.com"; // Test
const string baseUrl = "https://api.accp.tradecloud1.com"; // Acceptance


// Authentication
const string username = "your-username";
const string password = "your-password";

// Company settings (update if needed)
const string companyId = "your-company-id";
const string supplierAccountNumber = "your-supplier-account";
```

### Environment URLs

- **Development**: `https://branch.d.tradecloud1.com`
- **Test**:        `https://api.test.tradecloud1.com`
- **Acceptance**:  `https://api.accp.tradecloud1.com` (default)

Simply change the `baseUrl` constant to switch between environments. All API endpoints will be automatically constructed from this base URL.

## API Endpoints Used

All endpoints are constructed from the configured `baseUrl`:

1. **Authentication**: `{baseUrl}/v2/authentication/login`
2. **Send Order**: `{baseUrl}/v2/api-connector/order`
3. **Upload Document**: `{baseUrl}/v2/object-storage/document`
4. **Attach Documents**: `{baseUrl}/v2/api-connector/order/documents`
5. **Download ZIP**: `{baseUrl}/v2/object-storage/documents/zip` *(New feature)*

## Document Structure

The example creates the following documents:

### Header Document

- **File**: `order-header.txt`
- **Content**: General order information and terms
- **Attached to**: Order header

### Line Documents  

- **Files**: Mix of duplicate and unique names (to test ZIP handling)
  - Lines 1-3: `specification.txt` (duplicate names)
  - Lines 4-6: `manual.pdf` (duplicate names)
  - Lines 7-8: `drawing.dwg` (duplicate names)
  - Lines 9-10: `line-09-spec.txt`, `line-10-spec.txt` (unique names)
- **Content**: Line-specific specifications and instructions
- **Attached to**: Respective order lines (positions 0010-0100)

**Note**: The duplicate file names are intentional to test how the ZIP download API handles name conflicts (expected to add suffixes like -1, -2, etc.)

## Order Structure

The created order includes:

- **Order Number**: `PO-ZIP-TEST-{timestamp}`
- **Lines**: 10 lines with positions 0010, 0020, 0030, ..., 0100
- **Items**: TEST-ITEM-001 through TEST-ITEM-010
- **Quantities**: Progressive quantities (2, 4, 6, 8, ...)
- **Prices**: Progressive prices (11.50, 12.50, 13.50, ...)

## Output

The example will:

1. Create and send the order
2. Upload 11 documents and report their object IDs
3. Attach documents to the order/lines
4. Download a ZIP file named `order-documents-{timestamp}.zip`
5. Test concurrent downloads to verify rate limiting behavior

## ZIP Download API

The new ZIP download endpoint accepts a plain array of object IDs:

```json
[
  "object-id-1",
  "object-id-2",
  "object-id-3"
]
```

And returns a ZIP file containing all the requested documents.

## Running the Example

```bash
cd object-storage/DownloadZip
dotnet run
```

## Sample Output

```
Tradecloud comprehensive order with documents and ZIP download example.

=== Step 1: Creating order with 10 lines ===
Created order PO-ZIP-TEST-20241225123045 with 10 lines
SendOrder: status=200, elapsed=1250ms
Order PO-ZIP-TEST-20241225123045 sent successfully

=== Step 2: Creating sample documents ===
Created 11 sample documents (1 header + 10 line documents)
Duplicate file names created:
- specification.txt (lines 1-3)
- manual.pdf (lines 4-6)
- drawing.dwg (lines 7-8)
- Unique names (lines 9-10)

=== Step 3: Uploading documents ===
Uploading order-header.txt...
Upload order-header.txt: status=200, elapsed=450ms
Successfully uploaded order-header.txt with objectId: a1b2c3d4-...
...

=== Step 4: Attaching documents to order ===
Attaching 11 documents to order PO-ZIP-TEST-20241225123045...
AttachDocuments: status=200, elapsed=800ms
Successfully attached 11 documents to order PO-ZIP-TEST-20241225123045

=== Step 5: Downloading documents as ZIP ===
Requesting ZIP download for 11 documents...
DownloadZip request: status=200, elapsed=2100ms
Successfully downloaded ZIP file: order-documents-20241225-123045.zip (15847 bytes)
ZIP file contains 11 documents

=== Step 6: Testing concurrent ZIP downloads (rate limiting) ===
Starting 11 concurrent ZIP download requests...
All 11 concurrent requests completed in 3250ms

Results:
Request 01: 200 ✓ SUCCESS - 1250ms - 15847 bytes
Request 02: 200 ✓ SUCCESS - 1350ms - 15847 bytes
Request 03: 200 ✓ SUCCESS - 1450ms - 15847 bytes
Request 04: 200 ✓ SUCCESS - 1550ms - 15847 bytes
Request 05: 200 ✓ SUCCESS - 1650ms - 15847 bytes
Request 06: 200 ✓ SUCCESS - 1750ms - 15847 bytes
Request 07: 200 ✓ SUCCESS - 1850ms - 15847 bytes
Request 08: 200 ✓ SUCCESS - 1950ms - 15847 bytes
Request 09: 200 ✓ SUCCESS - 2050ms - 15847 bytes
Request 10: 200 ✓ SUCCESS - 2150ms - 15847 bytes
Request 11: 429 ⚠ RATE LIMITED - 450ms - 0 bytes

Summary:
- Successful downloads: 10
- Rate limited (429): 1
- Other errors: 0
✓ Rate limiting is working - got 1 429 responses

=== Process completed successfully! ===
```
