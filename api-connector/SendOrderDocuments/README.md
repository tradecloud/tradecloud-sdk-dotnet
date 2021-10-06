# Send Order Documents

This example sends an order document to Tradecloud using the API Connector

## Prerequisites

1. A Tradecloud user with `buyer` and `integration` roles
2. Upload a document using object-storage/UploadDocument
3. Issue an order using api-connector/SendOrder

## Configure

In the source code:
- amend authenticationUrl if necessary
- username on Tradecloud
- password on Tradecloud
- amend sendOrderDocumentsUrl if necessary
- replace the `objectId` in the source as returned from `2. Upload a document using object-storage/UploadDocument`
- replace `companyId` and `purchaseOrderNumber` in the source as used in `3. Issue an order using api-connector/SendOrder`

Amend order-documents.json if necessary

## Run

```
➜  api-connector git:(master) ✗ dotnet run
Tradecloud send order documents example.
Login response StatusCode: 200 ElapsedMilliseconds: 456
Login response Content: {"username":...}
SendOrderDocuments start=10/6/2021 7:42:02 PM elapsed=198ms status=200 reason=OK
SendOrderDocuments response body={"ok":true}
```