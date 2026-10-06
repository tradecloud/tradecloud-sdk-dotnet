# Send an order with 500 lines

Sends one purchase order whose lines are the `minimal-order.json` line repeated
to the API maximum of 500. Each run uses a new purchase order number.

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles, and a supplier account
matching `supplierAccountNumber` in `minimal-order.json`.

## Configure

Copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and
`TRADECLOUD_PASSWORD`. Amend `supplierAccountNumber` in `minimal-order.json`
for the environment you send to. `lineCount` in `SendOrder500Lines.cs` is 500.

## Run

```
dotnet run
```
