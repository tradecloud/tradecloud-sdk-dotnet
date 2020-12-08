# Send Order Indicators

This example sends order indicators to Tradecloud using the API Connector

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:

- amend authenticationUrl
- fill in username
- fill in password
- amend sendOrderIndicatorsUrl
- amend companyId when using super user, else remove companyId from the body
- amend order/lines indicators in the body

## Run

```
➜  SendOrderIndicators git:(master) ✗ dotnet run
Tradecloud send order indicators example.
Login response StatusCode: 200 ElapsedMilliseconds: 1316
Login response Content: {"username": ... }
SendOrderIndicators StatusCode: 200 ElapsedMilliseconds: 1526
SendOrderIndicators Body: {"ok":true}
```