# Poll shipments

This example polls shipments using the shipment service

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- fill in the search query

## Run

``` shell
➜  PollShipments git:(master) ✗ dotnet run
Tradecloud poll shipments example.
Login response StatusCode: 200 ElapsedMilliseconds: 606
Login response Content: ...
PollShipments start=4/8/2021 2:08:33 PM elapsed=58ms status=200 reason=OK
PollShipments response body={
  "data": [...],
  ...
```
