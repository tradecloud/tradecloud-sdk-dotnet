# Document Migration Tool

This tool migrates documents from the legacy Tradecloud system to Tradecloud One with **concurrent processing** and **automatic token refresh** for improved performance and reliability.

## Overview

The migration process:

1. **Find active orders** in Tradecloud One (orders with process status: Issued, InProgress, Confirmed, or Rejected; and logistics status: Open, Produced, ReadyToShip, or Shipped)
2. **Process orders concurrently** with controlled parallelism (max 3 concurrent orders)
3. **Find corresponding legacy orders** using the purchase order number
4. **Get detailed order information** using the order ID to check existing documents
5. **Migrate order header documents** that don't already exist in TC1 (checks each document individually)
6. **Migrate order line documents** that don't already exist in TC1 (checks each document individually)
7. **Automatically refresh access tokens** when they expire during long-running migrations

For each document, the tool:

- Checks if the document is already migrated (by comparing title/filename and checking for migration description)
- Downloads the document from the legacy system (only if not already migrated)
- Uploads it to Tradecloud One object storage
- Attaches it to the appropriate order or order line

**Key Features**:

- **Concurrent Processing**: Processes up to 3 orders simultaneously for faster migration
- **Token Refresh**: Automatically refreshes expired access tokens for long-running migrations
- **Idempotent**: Only migrates documents that don't already exist, allowing safe re-runs
- **Thread-Safe**: Proper synchronization for concurrent operations and logging

## Performance & Scalability

### Concurrent Processing

- **Max Concurrency**: 3 orders processed simultaneously
- **Conservative Limits**: Designed to prevent API rate limiting while maximizing throughput
- **Speed Improvement**: ~3x faster than sequential processing for large migrations
- **Thread-Safe Operations**: All API calls and logging are properly synchronized

### Token Management

- **Access Token Expiry**: Standard 1-hour expiration
- **Automatic Refresh**: Uses refresh token to extend migration beyond 1 hour
- **Retry Logic**: Automatically retries failed requests after token refresh
- **Thread-Safe Refresh**: Multiple concurrent operations safely share refreshed tokens

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
   REFRESH_TOKEN=your-refresh-token-here

   # Company Configuration
   BUYER_COMPANY_ID=your-buyer-company-id

   # Migration Configuration
   DRY_RUN=true
   ```

### Configuration Variables

| Variable | Description | Required | Notes |
|----------|-------------|----------|-------|
| `LEGACY_USERNAME` | Username for legacy Tradecloud system | Yes | |
| `LEGACY_PASSWORD` | Password for legacy Tradecloud system | Yes | Wrap in quotes if special chars |
| `ACCESS_TOKEN` | Bearer token for Tradecloud One API | Yes | Must be buyer company admin |
| `REFRESH_TOKEN` | Refresh token for long-running migrations | **Recommended** | Enables auto-refresh for migrations > 1 hour |
| `BUYER_COMPANY_ID` | Company ID of the buyer in Tradecloud One | Yes | |
| `DRY_RUN` | Set to `true` for testing, `false` for live migration | No | Defaults to `true` |

### Token Configuration

#### Access Token (Required)

- **Purpose**: Authenticates API requests to Tradecloud One
- **Expiration**: 1 hour
- **Requirement**: Must be a buyer company admin token (not support user)

#### Refresh Token (Recommended)

- **Purpose**: Automatically refreshes expired access tokens
- **Expiration**: 30 days (typically)
- **Benefits**: Enables migrations longer than 1 hour
- **Safety**: Without refresh token, long migrations will fail at 1-hour mark

### Security Notes

- The `.env` file is ignored by git to prevent accidentally committing credentials
- Never commit actual credentials to the repository
- The `env.example` file contains placeholder values for reference
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
- Display the current mode (DRY RUN or LIVE), buyer company ID, concurrency settings, and token refresh status
- Exit with an error if any required configuration is missing
- Process orders concurrently with progress reporting

### Example Output

```bash
Tradecloud document migration example
Running in DRY RUN mode
Buyer company ID: 12345
Max concurrency: 3
Token refresh: Available
Found 150 active orders to process
Processing 150 order groups with max 3 concurrent requests
Migration completed. Check log file: 20241201-143022-migrate-documents.log
Results: 148/150 orders successful, 245 documents would be migrated, 2 failures
```

### Dry Run Mode

For testing purposes, set `DRY_RUN=true` in the `.env` file. This will:

- Fetch all order and legacy data (to verify connectivity and access)
- Download documents from the legacy system (to verify permissions)
- Log what would be migrated without actually uploading or attaching documents
- Show document sizes and simulate object IDs for testing
- Process with full concurrency to test performance

## Output

The tool creates a detailed log file named `YYYYMMDD-HHMMSS-migrate-documents.log` with:

- Overall migration progress and mode (DRY RUN or LIVE)
- Buyer company ID, concurrency settings, and token refresh status
- Real-time progress with timestamps for concurrent operations
- Token refresh events (when applicable)
- For each order processed:
  - Purchase order number
  - Legacy order lookup results
  - Document migration details
- For each migrated document:
  - TC1 purchase order number and position (for order lines)
  - Legacy file ID, title, filename, and MIME type
  - TC1 object ID
  - Upload and attachment confirmation
- Final summary with success/failure counts and total documents migrated

## Processing Logic

### Concurrent Processing Flow

1. **Load and Group**: All orders are loaded and grouped for processing
2. **Semaphore Control**: SemaphoreSlim limits concurrent operations to 3
3. **Independent Processing**: Each order is processed independently
4. **Shared Resources**: Token refresh and logging are thread-safe
5. **Result Aggregation**: Results from all concurrent operations are collected

### Document Duplication Prevention

The tool uses a sophisticated approach to prevent document duplication:

1. **Detailed Order Retrieval**: For each order, the tool fetches complete order details using `/v2/order/{id}` (not the search result) to ensure it has the most up-to-date document information
2. **Individual Document Checking**: Each legacy document is checked against existing TC1 documents by comparing filenames
3. **Skip Already Migrated**: Documents that already exist are skipped with logging
4. **Process Only New Documents**: Only documents that don't already exist are downloaded, uploaded, and attached

### Token Refresh Logic

The tool automatically handles token expiration:

1. **Detection**: 401 Unauthorized responses trigger refresh logic
2. **Thread-Safe Refresh**: Double-check locking prevents multiple simultaneous refreshes
3. **Automatic Retry**: Failed requests are automatically retried with fresh tokens
4. **Logging**: Token refresh events are logged for monitoring

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

- **Concurrent Safety**: Individual order failures don't affect other concurrent operations
- **Token Expiry**: Automatic refresh and retry for 401 Unauthorized responses
- **API Errors**: Rate limiting and temporary failures are handled gracefully  
- **Document Skipping**: Orders/documents that already exist are safely skipped
- **Detailed Logging**: All errors are logged with context for troubleshooting
- **Idempotent**: The migration can be re-run safely multiple times
- **Graceful Degradation**: Individual failures don't stop the entire migration

## Performance Considerations

### Concurrency Settings

- **Default**: 3 concurrent orders (conservative for document operations)
- **Rationale**: Balances speed with API stability for document uploads
- **Customization**: Can be adjusted in code (`maxConcurrency` constant)

### Memory Usage

- **Document Buffering**: Documents are downloaded and uploaded in memory
- **Concurrent Operations**: Multiple documents may be in memory simultaneously
- **Large Files**: Monitor memory usage for migrations with large documents

### Network Considerations

- **Download Bandwidth**: Legacy system document downloads
- **Upload Bandwidth**: Tradecloud One document uploads
- **API Limits**: Conservative concurrency prevents rate limiting

## Troubleshooting

### Common Issues

#### Token Expiration

**Symptom**: Migration fails after ~1 hour with 401 errors
**Solution**: Configure `REFRESH_TOKEN` in `.env` file

#### Rate Limiting

**Symptom**: 429 Too Many Requests errors
**Solution**: Reduce `maxConcurrency` in code (default: 3)

#### Memory Issues

**Symptom**: OutOfMemoryException with large documents
**Solution**: Monitor memory usage, consider processing in smaller batches

#### Connection Timeouts

**Symptom**: Network timeouts during large file uploads
**Solution**: Check network stability, consider retry logic

### Debug Logging

The detailed log file includes:

- Timestamp for each operation
- Concurrent operation tracking
- Token refresh events
- Individual document processing details
- Error context and stack traces

## API Endpoints Used

### Tradecloud One APIs

- **Order Search**: `/v2/order-search/search` - Find active orders
- **Get Order**: `/v2/order/{id}` - Get detailed order information
- **Upload Document**: `/v2/object-storage/document` - Upload documents (returns `id` field)
- **Attach Order Document**: `/v2/order/{id}/document` - Attach to order header
- **Attach Order Line Document**: `/v2/order/{id}/line/{position}/document` - Attach to order line
- **Authentication**: `/v2/authentication/refresh` - Refresh expired tokens

### Legacy APIs

- **Find Order**: `/v1/purchaseOrder/tenant/{tenantId}/code/{code}` - Find legacy order
- **Find Order Line**: `/v1/purchaseOrderLine/{purchaseOrderLineId}` - Get detailed order line with documents
- **Get Order Document**: `/v1/purchaseOrder/{purchaseOrderId}/document/{fileId}` - Download header document
- **Get Order Line Document**: `/v1/purchaseOrderLine/{purchaseOrderLineId}/document/{fileId}` - Download line document

## Migration Strategy Recommendations

### For Large Migrations (1000+ orders)

- **Use Refresh Token**: Essential for migrations > 1 hour
- **Monitor Progress**: Check log files regularly
- **Run During Off-Hours**: Reduce load on production systems
- **Test First**: Always run with `DRY_RUN=true` initially

### For Small Migrations (< 100 orders)

- **Access Token Only**: May complete within 1 hour
- **Standard Concurrency**: Default settings work well
- **Quick Verification**: Results visible quickly

### For Very Large Migrations (10,000+ orders)

- **Consider Batching**: Split into multiple runs if needed
- **Monitor Memory**: Watch for memory usage patterns
- **Extended Monitoring**: Plan for multi-hour execution times

## Notes

- The tool only processes active orders (process status: Issued/InProgress/Confirmed/Rejected, logistics status: Open/Produced/ReadyToShip/Shipped)
- Legacy tenant ID corresponds to the Tradecloud One buyer company ID
- Legacy code corresponds to the Tradecloud One purchase order number
- Legacy row corresponds to the Tradecloud One order line position (with zero-padding conversion)
- Document uploads use the `filename` field from legacy documents
- Documents are uploaded with type "General" and description "Migrated from legacy system"
- Object storage requires buyer company admin authentication (not support user)
- Some newer orders may not exist in the legacy system (this is normal and logged)
- Concurrent processing is designed to be conservative to prevent API overload
- Token refresh requires the appropriate refresh token scope and permissions
