# Send Order

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
- amend sendOrderDocumentsUrl in necessary
- replace the `objectId` in the source as returned from `2. Upload a document using object-storage/UploadDocument`
- replace `companyId` and `purchaseOrderNumber` in the source as used in `3. Issue an order using api-connector/SendOrder`
- amend other order/lines fields

## Run

```
➜  api-connector git:(master) ✗ dotnet run
Tradecloud send order documents example.
Login response StatusCode: 200 ElapsedMilliseconds: 559
Login response Content:...
SendOrder StatusCode: 200 ElapsedMilliseconds: 165
SendOrder Body: {"ok":true}
```