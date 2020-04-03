# Upload document

This example uploads a document to the Tradecloud object storage.

## Configure

In the source code:
- username on Tradecloud
- password on Tradecloud
- path (for example `test.pdf`)

## Run

```
➜  tradecloud-api-dotnet-sdk git:(master) cd object-storage-upload-document 
➜  object-storage-upload-document git:(master) dotnet run
Tradecloud upload document example.
Authenticate StatusCode: 200
Authenticate Content: OK
Uploading document...please wait
UploadDocument StatusCode: 200
UploadDocument Content: {"id":"bc107c82-76de-4c21-a7c9-b1d2faa75b1e"}
```