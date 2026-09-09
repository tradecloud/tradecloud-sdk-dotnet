# Send forecast

This example sends a supplier forecast to Tradecloud using the API Connector

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- amend sendForecastUrl if necessary

Amend forecast.json if necessary:
- amend the `forecastNumber` 

## Run

```
➜ SendForecast git:(master) ✗ dotnet run
Tradecloud send forecast example.
Login response StatusCode: 200
Login response Content: ...
SendForecast StatusCode: 200
SendForecast Body: {"ok":true}
```