# Download Documents ZIP - 500MB Scale Test

This example demonstrates a comprehensive workflow for working with Tradecloud orders and documents with a 500MB scale test using a clever multiplication strategy:

## ⚡ Quick Setup (Required First)

**IMPORTANT:** Before running the test, you must generate the cached document template on Linux:

```bash
# Navigate to the DownloadZip directory
cd object-storage/DownloadZip/

# Generate a 10MB cached document (fast method using /dev/urandom)
dd if=/dev/urandom of=cached-document-10mb.bin bs=1M count=10

# Verify the file was created
ls -lh cached-document-10mb.bin

# Expected output: -rw-r--r-- 1 user user 10M [date] cached-document-10mb.bin
```

**Alternative methods:**

```bash
# Method 2: Using /dev/zero (creates file with zeros - faster)
dd if=/dev/zero of=cached-document-10mb.bin bs=1M count=10

# Method 3: Using fallocate (fastest, but may not work on all filesystems)
fallocate -l 10M cached-document-10mb.bin

# Method 4: Using head + /dev/urandom (more portable)
head -c 10485760 /dev/urandom > cached-document-10mb.bin
```

## Test Overview

This example demonstrates a comprehensive workflow for working with Tradecloud orders and documents:

1. **Create Order** - Creates an order with 9 lines
2. **Load Cached Documents** - Loads pre-generated 10MB template (10 documents total)
3. **Upload Documents** - Uploads 10 documents (1 header + 9 line documents, 10MB each)
4. **Attach Documents** - Attaches documents to the order (header document to order, line documents to respective lines)
5. **Multiply ObjectIds** - Creates ZIP request with each document included 5x (50 total entries)
6. **Request ZIP Download URL** - Requests a download URL for 50 entries (~500MB) using the ZIP API
7. **Download ZIP File** - Downloads the ~500MB ZIP file using the provided URL
8. **Test Rate Limiting** - Tests 6 concurrent ZIP URL requests to verify 429 responses when exceeding the 5 concurrent limit per user

## Features

- **500MB Scale Test** - Tests with 10 documents (10MB each) multiplied 5x for exactly 500MB ZIP
- **Clever Multiplication Strategy** - Upload only 100MB but include each document 5x in ZIP request
- **High Performance** - Uses pre-generated cached document template for instant startup  
- **Efficient Upload** - Only uploads ~100MB but tests exactly 500MB ZIP download
- **Timeout Optimized** - Reduced from 1GB to 500MB to avoid gateway timeouts
- Creates a complete purchase order with 9 order lines
- **One document per line** - 1 header document + 9 line documents (1 per order line, 10MB each)
- Uploads 10 documents (~100MB total) to Tradecloud object storage
- Attaches documents to the appropriate order/lines
- **Two-step ZIP download process** - First requests download URL, then downloads the ZIP file
- **Smart filename extraction** - Extracts filename from Content-Disposition headers when available
- **Duplicate filename handling** - Tests ZIP suffix handling with controlled duplicate filenames
- Tests 6 concurrent URL requests to verify 5-concurrent-user rate limiting (429 responses)
- **Bandwidth monitoring** - Records download time and calculates bandwidth in Mbps for large files
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
const string baseUrl = "https://tc-10397-download-zip-gcs-url.d.tradecloud1.com"; // Development
const string baseUrl = "https://api.test.tradecloud1.com"; // Test
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
- **Large file sizes (300-800 KB each) are designed to test bandwidth monitoring**

## Order Structure

The created order includes:

- **Order Number**: `PO-ZIP-TEST-{timestamp}`
- **Lines**: 9 lines with positions 0010, 0020, 0030, ..., 0090
- **Items**: TEST-ITEM-001 through TEST-ITEM-009
- **Quantities**: Progressive quantities (2, 4, 6, 8, ...)
- **Prices**: Progressive prices (11.50, 12.50, 13.50, ...)

## Output

The example will:

1. Create and send the order
2. Upload 10 documents and report their object IDs
3. Attach documents to the order/lines
4. Request a ZIP download URL for all documents
5. Download a ZIP file using the provided URL (filename extracted from response headers)
6. Test 6 concurrent URL requests to verify rate limiting behavior (max 5 per user)

## ZIP Download API

The ZIP download process now uses a **two-step approach**:

### Step 1: Request Download URL

POST to `/v2/object-storage/documents/zip` with:

```json
{
  "objectIds": [
    "object-id-1",
    "object-id-2",
    "object-id-3"
  ],
  "filename": "documents"
}
```

**Response:**

```json
{
  "downloadUrl": "https://storage.googleapis.com/bucket/file.zip?signature=..."
}
```

### Step 2: Download ZIP File

GET the `downloadUrl` to download the actual ZIP file. The response includes:

- **Content-Disposition header** with the actual filename (automatically extracted)
- **Binary ZIP content** containing all requested documents

**API Limits:**

- Maximum 500 documents per ZIP file
- Maximum 5 concurrent requests per user

## Running the Example

```bash
cd object-storage/DownloadZip
dotnet run
```

## Sample Output

```text
Tradecloud 500MB scale test: 10 documents (10MB each) with 5x ZIP multiplication.

=== Step 1: Creating order with 9 lines ===
Created order PO-ZIP-TEST-20241225123045 with 9 lines
SendOrder: status=200, elapsed=1250ms
Order PO-ZIP-TEST-20241225123045 sent successfully

=== Step 2: Creating sample documents ===
Creating 10 documents total: 1 header document + 9 line documents (10MB each)
Using cached document template for performance...
✓ Loaded cached document: cached-document-10mb.bin (10,485,760 bytes, 10.0 MB)
Creating 1 header document...
✓ Created header document using cached content
Creating 9 line documents (1 per line)...
  Created document for line 1/9
  Created document for line 2/9
  Created document for line 3/9
  Created document for line 4/9
  Created document for line 5/9
  Created document for line 6/9
  Created document for line 7/9
  Created document for line 8/9
  Created document for line 9/9

✓ Successfully created 10 documents using cached template:
   - 1 header document (10MB)
   - 9 line documents (10MB each)
📊 Upload size: 104,857,600 bytes (100 MB)
🔄 ZIP strategy: Each document will be included 5x = 50 total entries
📥 Expected ZIP download: ~500 MB
⚡ Performance: Instant creation using cached 10MB template

=== Step 3: Uploading documents ===
Uploading order-header-10mb.txt...
Upload order-header-10mb.txt: status=200, elapsed=2150ms
Successfully uploaded order-header-10mb.txt with objectId: a1b2c3d4-...
Uploading line-01-specification-10mb.txt...
Upload line-01-specification-10mb.txt: status=200, elapsed=2200ms
Successfully uploaded line-01-specification-10mb.txt with objectId: b2c3d4e5-...
...
Successfully uploaded 10 out of 10 documents

=== Step 4: Attaching documents to order ===
Preparing to attach 1 header documents and 9 line documents
Organized documents: 1 header + 9 lines with multiple documents each
Attaching 10 documents to order PO-ZIP-TEST-20241225123045...
AttachDocuments: status=200, elapsed=800ms
Successfully attached 10 documents to order PO-ZIP-TEST-20241225123045

=== Step 5: Requesting ZIP download URL with 5x multiplication ===
Creating ZIP with 10 unique documents × 5 copies = 50 total entries
Requesting ZIP download URL for 50 documents with filename: order-documents-20241225-123045.zip containing 50 documents
ZIP URL request: status=200, request time=150ms
Received download URL: https://storage.googleapis.com/bucket/file.zip?signature=...

=== Step 6: Downloading ZIP file from URL ===
Downloading ZIP file from provided URL (will extract actual filename from response, fallback: order-documents-20241225-123045.zip)
ZIP download: status=200, download time=25000ms
Extracted filename from Content-Disposition header: order-documents-20241225-123045.zip
Successfully downloaded ZIP file: order-documents-20241225-123045.zip
File size: 524,288,000 bytes (512000.00 KB, 500.00 MB)
Download time: 25000ms
Download bandwidth: 20,971,520 bytes/sec (167.77 Mbps)

=== Process completed successfully! ===
```
