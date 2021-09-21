# API v1 Get Orders

This example fetches unacknowledged (either new or updated) orders from the API v1

## Prerequisites

A tradecloud.nl user

## Configure

In the source code:
- fill in username on tradecloud.nl
- fill in password on tradecloud.nl

## Run
```
➜  GetUnacknowledgedOrders git:(master) ✗ dotnet run
Tradecloud get unacknowledged orders example.
GetUnacknowledgedOrders status=200 reason=OK
GetUnacknowledgedOrders response body={
  "totalPages": 1,
  "pageSize": 10,
  "data": [
    { ...
    }
  ],
  "page": 1,
  "total": 5
}
```