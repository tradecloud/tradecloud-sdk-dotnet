# API v1 Get Orders

This example fetches unacknowledged (either new or updated) orders from the API v1

The buyer can use polling to check for new or updated confirmations periodically. 

The supplier can use polling to check for new or updated orders periodically.

## Prerequisites

A tradecloud.nl buyer or supplier user

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password

## Run
```
➜  api-v1 git:(master) ✗ cd GetUnacknowledgedOrders 
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
