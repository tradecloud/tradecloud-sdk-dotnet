# Send Order Indicators

This example sends order indicators to Tradecloud using the API Connector

## Prerequisites

1. A Tradecloud user with `buyer` and `integration` roles
2. Issue an order using api-connector/SendOrder

## Configure

In the source code:

- amend authenticationUrl
- fill in username
- fill in password
- amend sendOrderIndicatorsUrl

Amend order-indicators.json if necessary:
- replace the `purchaseOrderNumber` as used in `2. Issue an order using api-connector/SendOrder`

## Run

```
➜  SendOrderIndicators git:(master) ✗ dotnet run
Tradecloud send order indicators example.
Login response StatusCode: 200 ElapsedMilliseconds: 1316
Login response Content: {"username": ... }
SendOrderIndicators StatusCode: 200 ElapsedMilliseconds: 1526
SendOrderIndicators Body: {"ok":true}
```