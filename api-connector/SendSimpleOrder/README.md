# Send Order

This example sends a simple order to Tradecloud using the API Connector

A simple order
- uses `scheduledDelivery` (one per order line) in stead of a `deliverySchedule`
- uses `actualDelivery` (zero or one per order line) in stead of a `deliveryHistory`
- `chargeLines` are not supported

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendOrderUrl if necessary

Amend `simple-order.json`` if necessary:
- amend the `purchaseOrderNumber` 

## Run

```
➜  api-connector git:(master) ✗ dotnet run
Tradecloud send order example.
Login response StatusCode: 200
Login response Content: ...
SendOrder StatusCode: 200
SendOrder Body: {"ok":true}
```