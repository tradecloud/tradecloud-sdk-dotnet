# Search Orders

This example searches for orders using the order-search service

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- fill in the search query

## Run

``` shell
➜  SearchOrders git:(master) ✗ dotnet run
Tradecloud search orders example.
Login response StatusCode: 200 ElapsedMilliseconds: 606
Login response Content: ...
SendOrder start=4/8/2021 2:08:33 PM elapsed=58ms status=200 reason=OK
SendOrder response body={
  "data": [...],
  ...
```
