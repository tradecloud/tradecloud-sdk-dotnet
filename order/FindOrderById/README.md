# Find order by id

This example finds an order by Tradecloud id from the order service

## Configure

In the source code:

- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- orderId on Tradecloud

## Run

``` shell
➜  FindOrderById git:(master) ✗ dotnet run      
Tradecloud find order by id example.
Login response StatusCode: 200 ElapsedMilliseconds: 508
Login response Content: {...}
FindOrderById start=9/1/2021 10:16:17 PM elapsed=769ms status=200 reason=OK
FindOrderById response body={
  "id": "...",
  ...
```
