# Seed forecasts

This example seeds a bunch of supplier forecasts in Tradecloud using the API Connector
If you want to send one supplier forecast, check `SendForcast` instead.

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendForecastUrl if necessary

Amend forecast.json or line.json if necessary.

## Run

```
➜  SeedForecasts git:(master) ✗ dotnet run
Tradecloud send forecast example.
Login response StatusCode: 200
Login response Content: ...
SeedForecasts ...
```