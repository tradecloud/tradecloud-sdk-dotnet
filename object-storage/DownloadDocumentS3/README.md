# Download document

This example downloads a document directly from Amazon AWS S3 using the downloadUrl in GetDocumentMetadata 

## Prerequisites

A Tradecloud user with `buyer` or `supplier` role.

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend the objectStorageDocumentUrl if necessary
- set the objectId

## Run

```
➜  DownloadDocumentS3 git:(master) dotnet run
Tradecloud download document example.
Login response StatusCode: 200 ElapsedMilliseconds: 418
Login response Content: {"username": ...
GetDocumentMetadata start=1/21/2022 10:34:09 PM elapsed=29ms status=200 reason=OK
GetDocumentMetadata response body={
  "id": ...
  "filename": "...
  "contentType": "application/pdf",
  "downloadUrl": ...
}
DownloadDocument ... please wait
DownloadDocument start=1/21/2022 10:34:09 PM elapsed=475ms

```