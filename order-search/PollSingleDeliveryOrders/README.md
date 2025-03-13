# Poll Orders

This example polls single delivery orders using the order-search service

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- fill in the search query

## Run

```shell
➜  PollOrders git:(master) ✗ dotnet run
Tradecloud poll single delivery orders example.
Login response StatusCode: 200 ElapsedMilliseconds: 606
Login response Content: ...
PollSingleDeliveryOrders start=4/8/2021 2:08:33 PM elapsed=58ms status=200 reason=OK
PollSingleDeliveryOrders response body={
  "data": [...],
  ...
```
