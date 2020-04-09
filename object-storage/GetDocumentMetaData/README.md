# Get document meta data
This example gets the document meta data from the Tradecloud object storage.

## Configure

In the source code:
- username on Tradecloud
- password on Tradecloud
- getDocumentMetadataUrl including objectId

## Run

```
➜  GetDocumentMetaData git:(master) ✗ dotnet run
Tradecloud get document metadata example.
Authenticate StatusCode: 200
Authenticate Content: OK
GetDocumentMetadata StatusCode: 200
GetDocumentMetadata Content: {
  "id": "67aa8ece-5d41-496f-a94c-483e360b833b",
  "filename": "test.pdf",
  "contentType": "application/octet-stream",
  "downloadUrl": "https://tradecloud-accp-documents.s3.eu-central-1.amazonaws.com/67aa8ece-5d41-496f-a94c-483e360b833b..."
}
```