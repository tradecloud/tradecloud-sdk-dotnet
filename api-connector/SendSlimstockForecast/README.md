# Send Slimstock forecast

This example sends a Slimstock supplier forecast to Tradecloud using the API Connector

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendForecastUrl if necessary

Amend slimstock_forecast.json if necessary:
- amend the `forecastNumber` 

## Run

```
➜ SendSlimstockForecast git:(master) ✗ dotnet run
Tradecloud send Slimstock forecast example.
Login response StatusCode: 200
Login response Content: ...
SendSlimstockForecast StatusCode: 200
SendSlimstockForecast Body: {"ok":true}
```