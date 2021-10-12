# Attach Order Documents

This example sends an order document to Tradecloud using the API Connector

## Prerequisites

1. A Tradecloud user with `buyer` and `integration` roles
2. Issue an order using api-connector/SendOrder
3. Upload a document using object-storage/UploadDocument

## Configure

In the source code:
- amend authenticationUrl if necessary
- username on Tradecloud
- password on Tradecloud
- amend sendOrderDocumentsUrl if necessary

Amend order-documents.json if necessary:
- replace the `purchaseOrderNumber` as used in `2. Issue an order using api-connector/SendOrder`
- replace the `objectId` s returned from `3. Upload a document using object-storage/UploadDocument`

## Run

```
➜  api-connector git:(master) ✗ dotnet run
Tradecloud attach order documents example.
Login response StatusCode: 200 ElapsedMilliseconds: 456
Login response Content: {"username":...}
AttachOrderDocuments start=10/6/2021 7:42:02 PM elapsed=198ms status=200 reason=OK
AttachOrderDocuments response body={"ok":true}
```