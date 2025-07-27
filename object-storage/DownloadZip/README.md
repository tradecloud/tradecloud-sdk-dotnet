# Download Documents ZIP

This example demonstrates a comprehensive workflow for working with Tradecloud orders and documents:

1. **Create Order** - Creates an order with 10 lines 
2. **Upload Documents** - Uploads 11 documents (1 header document + 10 line documents)
3. **Attach Documents** - Attaches documents to the order (header document to order, line documents to respective lines)
4. **Download ZIP** - Downloads all documents as a single ZIP file using the new getDocumentsZip API
5. **Test Rate Limiting** - Tests 6 concurrent ZIP downloads to verify 429 responses when exceeding the 5 concurrent limit per user

## Features

- Creates a complete purchase order with 10 order lines
- Generates sample documents for testing (text files with relevant content)
- Uploads documents to Tradecloud object storage
- Attaches documents to the appropriate order/lines
- Downloads all documents in a single ZIP file
- Tests 6 concurrent downloads to verify 5-concurrent-user rate limiting (429 responses)
- **Bandwidth monitoring** - Records download time and calculates bandwidth to verify 1 MiB/s bandwidth limiting
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

- **File**: `order-header.txt` (500 KB)
- **Content**: Large document with general order information and detailed technical data
- **Attached to**: Order header

### Line Documents  

- **Files**: Mix of duplicate and unique names (to test ZIP handling)
  - Lines 1-3: `specification.txt` (350-450 KB each - duplicate names)
  - Lines 4-6: `manual.pdf` (500-650 KB each - duplicate names)
  - Lines 7-8: `drawing.dwg` (700-750 KB each - duplicate names)
  - Lines 9-10: `line-09-spec.txt`, `line-10-spec.txt` (800 KB each - unique names)
- **Content**: Large line-specific documents with technical specifications, quality control data, manufacturing details
- **Total Size**: ~6-7 MB uncompressed (varies due to ZIP compression)
- **Attached to**: Respective order lines (positions 0010-0100)

**Notes**: 
- The duplicate file names are intentional to test how the ZIP download API handles name conflicts (expected to add suffixes like -1, -2, etc.)
- **Large file sizes (300-800 KB each) are designed to test 1 MiB/s bandwidth limiting**

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
5. Test 6 concurrent downloads to verify rate limiting behavior (max 5 per user)

## ZIP Download API

The new ZIP download endpoint accepts a request object with document IDs and filename:

```json
{
  "objectIds": [
    "object-id-1",
    "object-id-2",
    "object-id-3"
  ],
  "filename": "documents.zip"
}
```

**API Limits:**

- Maximum 500 documents per ZIP file
- Maximum 5 concurrent downloads per user per service instance

And returns a ZIP file containing all the requested documents with the specified filename.

## Bandwidth Limiting Testing

**Expected Bandwidth Limit**: 1 MiB/s (1,048,576 bytes/sec) per download

**What to Look For**:
- Single downloads should show bandwidth close to but not exceeding 1 MiB/s
- Concurrent downloads should each be throttled to ~1 MiB/s 
- If downloads are significantly faster, bandwidth limiting may not be active
- If downloads are significantly slower, there may be network constraints

**File Sizes**: Large files (300-800 KB each, ~6 MB total ZIP) ensure download times are long enough to observe bandwidth limiting effects.

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
Creating large test documents to test bandwidth limiting...
Created header document: 512,000 bytes
Created line 1 document: 358,400 bytes
Created line 2 document: 409,600 bytes
Created line 3 document: 460,800 bytes
Created line 4 document: 512,000 bytes
Created line 5 document: 563,200 bytes
Created line 6 document: 614,400 bytes
Created line 7 document: 716,800 bytes
Created line 8 document: 768,000 bytes
Created line 9 document: 819,200 bytes
Created line 10 document: 819,200 bytes
Created 11 sample documents (1 header + 10 line documents)
Total size: 6,553,600 bytes (6400.0 KB, 6.3 MB)
Duplicate file names created:
- specification.txt (lines 1-3)
- manual.pdf (lines 4-6)
- drawing.dwg (lines 7-8)
- Unique names (lines 9-10)
Large files should help test 1 MiB/s bandwidth limiting!

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
Requesting ZIP download for 11 documents with filename: order-documents-20241225-123045.zip
DownloadZip request: status=200, request time=850ms
Successfully downloaded ZIP file: order-documents-20241225-123045.zip
File size: 4,234,567 bytes (4135.32 KB, 4.04 MB)
Total time: 5150ms (request: 850ms, download: 4300ms)
Total bandwidth: 822,257 bytes/sec (0.784 MiB/s)
Download bandwidth: 984,552 bytes/sec (0.939 MiB/s)
ZIP file contains 11 documents
NOTE: Download bandwidth approaching 1 MiB/s limit

=== Step 6: Testing concurrent ZIP downloads (rate limiting) ===
Starting 6 concurrent ZIP download requests (API limit: 5 concurrent per user)...
All 6 concurrent requests completed in 2850ms

Results:
Request 01: 200 ✓ SUCCESS - 4250ms - 4,234,567 bytes - 996,014 bytes/sec (0.950 MiB/s)
Request 02: 200 ✓ SUCCESS - 4350ms - 4,234,567 bytes - 973,243 bytes/sec (0.928 MiB/s)
Request 03: 200 ✓ SUCCESS - 4450ms - 4,234,567 bytes - 951,361 bytes/sec (0.907 MiB/s)
Request 04: 200 ✓ SUCCESS - 4550ms - 4,234,567 bytes - 930,289 bytes/sec (0.887 MiB/s)
Request 05: 200 ✓ SUCCESS - 4650ms - 4,234,567 bytes - 910,552 bytes/sec (0.868 MiB/s)
Request 06: 429 ⚠ RATE LIMITED - 450ms - 0 bytes

Summary:
- Successful downloads: 5
- Rate limited (429): 1
- Other errors: 0

Bandwidth Statistics (successful downloads):
- Average: 952,292 bytes/sec (0.908 MiB/s)
- Minimum: 910,552 bytes/sec (0.868 MiB/s)
- Maximum: 996,014 bytes/sec (0.950 MiB/s)
✓ Consistent bandwidth performance near 1 MiB/s limit (8.5% variation)
NOTE: All downloads staying close to 1 MiB/s indicates bandwidth limiting is active

✓ Rate limiting is working - got 1 429 responses
✓ API correctly enforces 5 concurrent downloads per user limit

=== Process completed successfully! ===
```
