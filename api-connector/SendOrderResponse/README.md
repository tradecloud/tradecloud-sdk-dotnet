# Send Order Response

This example sends an order response to Tradecloud using the API Connector

## Prerequisites

A Tradecloud user with `supplier` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendOrderResponseUrl in necessary

Amend order-indicators.json if necessary:
- replace the `purchaseOrderNumber` as used in `2. Issue an order using api-connector/SendOrder`

## Run

``` shell
➜  SendOrderResponse git:(master) ✗ dotnet run
Tradecloud send order response example.
Login response StatusCode: 200 ElapsedMilliseconds: 446
Login response Content: {...}
SendOrderResponse start=10/11/2021 9:22:50 PM elapsed=207ms status=200 reason=OK
SendOrderResponse response body={
  "ok": true
}
```
