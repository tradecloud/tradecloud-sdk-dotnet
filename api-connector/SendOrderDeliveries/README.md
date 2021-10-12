# Send Order deliveries

This example sends order deliveries to Tradecloud using the API Connector

## Prerequisites

1. A Tradecloud user with `buyer` and `integration` roles
2. Issue an order using api-connector/SendOrder

## Configure

In the source code:

- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendOrderUrl in necessary
- amend order/lines fields

Amend order-deliveries.json if necessary:
- replace the `purchaseOrderNumber` as used in `2. Issue an order using api-connector/SendOrder`

## Run

``` shell
➜  SendOrderDeliveries git:(master) ✗ dotnet run
Tradecloud send order deliveries example.
Login response StatusCode: 200 ElapsedMilliseconds: 457
Login response Content: {...}
SendOrderDelveries start=10/11/2021 9:00:43 PM elapsed=66ms status=200 reason=OK
SendOrderDelveries response body={
  "ok": true
}
```
