# Find order by id

This example finds a shipment by Tradecloud id from the shipment service

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- shipmentId on Tradecloud

## Run

``` shell
➜  FindShipmentById git:(master) ✗ dotnet run      
Tradecloud find shipment by id example.
Login response StatusCode: 200 ElapsedMilliseconds: 527
Login response Content: {"username": ... }
FindShipmentById start=3/16/2022 1:24:51 PM elapsed=239ms status=200 reason=OK
FindShipmentById response body={
  "id": ...
}
```
