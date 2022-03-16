# Find order by id

This example resends a shipment to the buyer or supplier ERP/WMS system

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- shipmentId on Tradecloud

## Run

``` shell
➜  ResendShipment git:(master) ✗ dotnet run      
Tradecloud resend shipment example.
Login response StatusCode: 200 ElapsedMilliseconds: 395
Login response Content: {"username": ... }
ResendShipment start=3/16/2022 1:57:43 PM elapsed=47ms status=200 reason=OK
ResendShipment response body={
  "ok": true
}
```
