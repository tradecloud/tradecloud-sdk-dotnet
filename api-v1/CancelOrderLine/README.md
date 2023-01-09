# API v1 Confirm Order Line

This example cancels one order line on the API v1

## Prerequisites

- A tradecloud.nl supplier user
- An order line to cancel

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password
- fill in the order LINE id in the URL
## Run

```
➜  CancelOrderLine git:(master) ✗ dotnet run
Tradecloud cancel order line example.
CancelOrderLine status=200 reason=OK
CancelOrderLine response body={
  ...
```
