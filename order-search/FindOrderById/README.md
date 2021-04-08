# Find order by id

This example finds an order by Tradecloud id from the order-search service

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- orderId on Tradecloud

## Run

``` shell
➜  FindOrderById git:(master) ✗ dotnet run      
Tradecloud search orders example.
Login response StatusCode: 200 ElapsedMilliseconds: 638
Login response Content: ...
FindOrderById start=4/8/2021 2:11:18 PM elapsed=35ms status=200 reason=OK
FindOrderById response body={
  "id": "...",
  ...
```
