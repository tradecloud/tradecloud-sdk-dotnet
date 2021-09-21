# API v1 Send Order

This example sends one order to the API v1

The buyer can send either a new order or an order update.

## Prerequisites

A tradecloud.nl buyer user

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password
- check and amend order.json
## Run

```
➜  SendOrder git:(master) ✗ dotnet run
Tradecloud send order example.
ConfirmOrderLine status=200 reason=OK
ConfirmOrderLine response body={
  ...
```