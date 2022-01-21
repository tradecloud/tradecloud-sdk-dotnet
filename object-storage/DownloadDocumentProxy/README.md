# Download document

This example downloads a document via the Tradecloud object storage proxy.

The prefered way is to download directly from Amazon AWS S3 using the downloadUrl in GetDocumentMetadata 

Downloading via the object storage is a fallback when Amazon AWS S3 is not reachable for you.

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
➜  DownloadDocumentProxy git:(master) dotnet run
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