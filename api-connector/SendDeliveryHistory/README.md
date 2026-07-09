# Send order delivery history

This example replaces the delivery history of one or multiple order lines on Tradecloud using the API Connector.

The existing `deliveryHistory` of an order line is *replaced* by the list you send. Send the complete current delivery history as known in your ERP system; previously recorded deliveries that are not included will be removed. An empty array `[]` clears all existing delivery history lines for the order line. The total number of delivery history lines is limited to 100 lines per order line.

## Prerequisites

1. A Tradecloud user with `buyer` and `integration` roles
2. Issue an order using api-connector/SendOrder

## Configure

In the source code:

- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendDeliveryHistoryUrl if necessary

Amend delivery-history.json if necessary:

- replace the `purchaseOrderNumber` as used in `2. Issue an order using api-connector/SendOrder`
- amend the line `position` and `deliveryHistory` entries

## Run

``` shell
➜  SendDeliveryHistory git:(master) ✗ dotnet run
Tradecloud send order delivery history example.
Login response StatusCode: 200 ElapsedMilliseconds: 457
Login response Content: {...}
SendDeliveryHistory start=07/09/2026 10:00:43 AM elapsed=66ms status=200 reason=OK
SendDeliveryHistory response body=
```
