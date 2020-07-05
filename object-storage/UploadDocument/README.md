# Upload document

This example uploads a document to the Tradecloud object storage.

## Prerequisites

A Tradecloud user with `buyer` or `supplier` role

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendOrderUrl in necessary
- fill in document path (for example `test.pdf`)

## Run

```
➜  UploadDocument git:(master) dotnet run
Tradecloud upload document example.
Authenticate StatusCode: 200
Authenticate Content: OK
Uploading document...please wait
UploadDocument StatusCode: 200
UploadDocument Content: {"id":"67aa8ece-5d41-496f-a94c-483e360b833b"}
```