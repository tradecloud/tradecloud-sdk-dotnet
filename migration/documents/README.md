# Document Migration Tool

This tool migrates documents from the legacy Tradecloud system to Tradecloud One.

## Overview

The migration process:

1. **Find active orders** in Tradecloud One (orders with process status: Issued, InProgress, Confirmed, or Rejected; and logistics status: Open, Produced, ReadyToShip, or Shipped)
2. **Find corresponding legacy orders** using the purchase order number
3. **Get detailed order information** using the order ID to check existing documents
4. **Migrate order header documents** that don't already exist in TC1 (checks each document individually)
5. **Migrate order line documents** that don't already exist in TC1 (checks each document individually)

For each document, the tool:

- Checks if the document is already migrated (by comparing title/filename and checking for migration description)
- Downloads the document from the legacy system (only if not already migrated)
- Uploads it to Tradecloud One object storage
- Attaches it to the appropriate order or order line

**Key Feature**: The tool only migrates documents that don't already exist, allowing it to be run multiple times safely without creating duplicates.

## Configuration

The migration tool uses environment variables for configuration. Before running the migration, you need to configure these variables using a `.env` file.

### Setup Configuration

1. **Copy the example file**: Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. **Edit the `.env` file** with your actual credentials and configuration:

   ```bash
   # Legacy Tradecloud System Configuration
   LEGACY_USERNAME=your-legacy-username@example.com
   LEGACY_PASSWORD="password-with-\"quotes\"-and-special-chars"

   # Tradecloud One Configuration
   # Must be a buyer company admin token, not a support user token
   ACCESS_TOKEN=your-bearer-token-here

   # Company Configuration
   BUYER_COMPANY_ID=your-buyer-company-id

   # Migration Configuration
   DRY_RUN=true
   ```

### Configuration Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `LEGACY_USERNAME` | Username for legacy Tradecloud system | Yes |
| `LEGACY_PASSWORD` | Password for legacy Tradecloud system | Yes |
| `ACCESS_TOKEN` | Bearer token for Tradecloud One API (must be buyer company admin) | Yes |
| `BUYER_COMPANY_ID` | Company ID of the buyer in Tradecloud One | Yes |
| `DRY_RUN` | Set to `true` for testing (no uploads/attachments), `false` for live migration | No (defaults to `true`) |

### Security Notes

- The `.env` file is ignored by git to prevent accidentally committing credentials
- Never commit actual credentials to the repository
- The `.env.example` file contains placeholder values for reference
- **Special Characters**: If your password contains special characters (quotes, spaces, etc.), wrap it in double quotes and escape internal quotes with backslashes:
  ```bash
  LEGACY_PASSWORD="password-with-\"quotes\"-and-special-chars"
  ```

## Usage

1. **Configure environment variables** as described in the Configuration section above
2. **Build the project**:

   ```bash
   dotnet build
   ```

3. **Run the migration**:

   ```bash
   dotnet run
   ```

The tool will automatically:

- Load configuration from the `.env` file
- Validate that all required variables are present
- Display the current mode (DRY RUN or LIVE) and buyer company ID
- Exit with an error if any required configuration is missing

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

### Document Duplication Prevention

The tool uses a sophisticated approach to prevent document duplication:

1. **Detailed Order Retrieval**: For each order, the tool fetches complete order details using `/v2/order/{id}` (not the search result) to ensure it has the most up-to-date document information
2. **Individual Document Checking**: Each legacy document is checked against existing TC1 documents by comparing filenames
3. **Skip Already Migrated**: Documents that already exist are skipped with logging
4. **Process Only New Documents**: Only documents that don't already exist are downloaded, uploaded, and attached

### Document Location in JSON

- **TC1 order header documents**: `order.buyerOrder.documents[]` (from detailed order)
- **TC1 order line documents**: `order.lines[].buyerLine.documents[]` (from detailed order)
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