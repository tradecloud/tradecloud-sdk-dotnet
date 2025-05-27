# Document Migration Tool

This tool migrates documents from the legacy Tradecloud system to Tradecloud One.

## Overview

The migration process:

1. **Find active orders** in Tradecloud One (orders with process status: Issued, InProgress, Confirmed, or Rejected; and logistics status: Open, Produced, ReadyToShip, or Shipped)
2. **Find corresponding legacy orders** using the purchase order number
3. **Migrate order header documents** if they don't already exist in TC1
4. **Migrate order line documents** if they don't already exist in TC1

For each document, the tool:

- Downloads the document from the legacy system
- Uploads it to Tradecloud One object storage
- Attaches it to the appropriate order or order line

## Configuration

Before running the migration, you need to configure the following constants in `MigrateDocuments.cs`:

### Legacy System Configuration

```csharp
const string legacyUsername = "your-legacy-username";
const string legacyPassword = "your-legacy-password";
```

### Tradecloud One Configuration

**Important**: The access token must be from a buyer company admin user, not a support user, otherwise object storage authorization will fail.

```csharp
const string accessToken = "your-bearer-token";  // Must be buyer company admin
```

### Buyer Company Configuration

```csharp
const string buyerCompanyId = "your-buyer-company-id";
```

### Dry Run Configuration

```csharp
const bool dryRun = false;  // Set to true for testing (no uploads/attachments)
```

## Usage

1. Configure the credentials and company ID as described above
2. Build the project:

   ```bash
   dotnet build
   ```

3. Run the migration:

   ```bash
   dotnet run
   ```

### Dry Run Mode

For testing purposes, set `dryRun = true` in the code. This will:

- Fetch all order and legacy data (to verify connectivity and access)
- Download documents from the legacy system (to verify permissions)
- Log what would be migrated without actually uploading or attaching documents
- Show document sizes and simulate object IDs for testing

## Output

The tool creates a detailed log file named `YYYYMMDD-HHMMSS-migrate-documents.log` with:

- Overall migration progress and mode (DRY RUN or LIVE)
- Buyer company ID being processed
- For each order processed:
  - Purchase order number
  - Legacy order lookup results
  - Document migration details
- For each migrated document:
  - TC1 purchase order number and position (for order lines)
  - Legacy file ID, title, filename, and MIME type
  - TC1 object ID
  - Upload and attachment confirmation

## Processing Logic

### Document Location in JSON

- **TC1 order header documents**: `order.buyerOrder.documents[]`
- **TC1 order line documents**: `order.lines[].buyerLine.documents[]`
- **Legacy order header documents**: `order.documents[]`
- **Legacy order line documents**: Retrieved via separate API call to `/purchaseOrderLine/{id}`

### Position Matching

- **TC1 positions**: Zero-padded format (e.g., "00010", "00020")
- **Legacy row numbers**: Non-padded format (e.g., "10", "20")
- The tool automatically handles the conversion using `TrimStart('0')`

### Document Attachment Format

Documents are attached using the correct API format:

```json
{
  "documents": [
    {
      "objectId": "uploaded-object-id",
      "name": "document-title",
      "type": "General",
      "description": "Migrated from legacy system"
    }
  ],
  "addedByCompanyId": "buyer-company-id"
}
```

## Error Handling

- The tool skips orders that already have documents (prevents duplicates)
- Individual document failures don't stop the entire migration
- All errors are logged with details for troubleshooting
- The migration can be re-run safely (idempotent)
- Logs when legacy orders/lines have no documents to migrate

## API Endpoints Used

### Tradecloud One APIs

- **Order Search**: `/v2/order-search/search` - Find active orders
- **Upload Document**: `/v2/object-storage/document` - Upload documents (returns `id` field)
- **Attach Order Document**: `/v2/order/{id}/document` - Attach to order header
- **Attach Order Line Document**: `/v2/order/{id}/line/{position}/document` - Attach to order line

### Legacy APIs

- **Find Order**: `/v1/purchaseOrder/tenant/{tenantId}/code/{code}` - Find legacy order
- **Find Order Line**: `/v1/purchaseOrderLine/{purchaseOrderLineId}` - Get detailed order line with documents
- **Get Order Document**: `/v1/purchaseOrder/{purchaseOrderId}/document/{fileId}` - Download header document
- **Get Order Line Document**: `/v1/purchaseOrderLine/{purchaseOrderLineId}/document/{fileId}` - Download line document

## Notes

- The tool only processes active orders (process status: Issued/InProgress/Confirmed/Rejected, logistics status: Open/Produced/ReadyToShip/Shipped)
- Legacy tenant ID corresponds to the Tradecloud One buyer company ID
- Legacy code corresponds to the Tradecloud One purchase order number
- Legacy row corresponds to the Tradecloud One order line position (with zero-padding conversion)
- Document uploads use the `filename` field from legacy documents
- Documents are uploaded with type "General" and description "Migrated from legacy system"
- Object storage requires buyer company admin authentication (not support user)
- Some newer orders may not exist in the legacy system (this is normal and logged) 