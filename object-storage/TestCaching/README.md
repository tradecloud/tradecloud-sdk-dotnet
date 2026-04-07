# Test Caching (TC-10937)

Black-box test for object-storage caching: upload deduplication and presigned download URL caching.

## What it tests

### Test 1: Upload Deduplication

Uploads the same file content twice and compares the returned `documentId` values.

- **CACHED**: both uploads return the same `documentId` (server recognized duplicate via content hash)
- **NOT CACHED**: different `documentId`s (each upload created a new S3 object)

### Test 2: Presigned Download URL Caching

Requests `GET /v2/object-storage/document/{id}/metadata` twice in quick succession and compares the `downloadUrl`.

- **CACHED**: same presigned URL returned (server served from cache)
- **NOT CACHED**: different presigned URLs (server called S3 presigner each time)

### Test 2b: TTL Expiry (optional)

Waits 16 minutes (TTL = 15 min) and requests metadata a 3rd time.

- **NEW URL**: cache expired correctly
- **STILL CACHED**: cache did not expire (unexpected)

## Prerequisites

Fill in `username` and `password` in `TestCaching.cs`.

## Run

```shell
dotnet run
```

With TTL expiry test (takes ~16 minutes):

```shell
dotnet run -- --wait-for-expiry
```

## Expected output

```shell
=== Tradecloud Object Storage Caching Test (TC-10937) ===

Authenticated successfully.

--- Test 1: Upload Deduplication ---
Uploading the same file content twice.

  Upload #1: status=200 elapsed=...ms
  Upload #2: status=200 elapsed=...ms

  Upload #1 documentId: abc123
  Upload #2 documentId: abc123
  RESULT: CACHED - same documentId returned (upload was deduplicated)

--- Test 2: Presigned Download URL Caching ---
  documentId: abc123

  downloadUrl #1: https://s3...
  downloadUrl #2: https://s3...
  RESULT: CACHED - same presigned URL returned
```
