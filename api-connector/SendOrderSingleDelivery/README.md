# Send Order

This example sends a single delivery order to Tradecloud using the API Connector

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- amend sendSingleDeliveryOrderUrl if necessary

Amend order.json if necessary:
- amend the `purchaseOrderNumber` 

## Run

```
➜  SendOrderSingleDelivery git:(master) ✗ dotnet run
Tradecloud send single delivery order example.
Login response StatusCode: 200
Login response Content: ...
SendOrder StatusCode: 200
SendOrder Body: {"ok":true}
```