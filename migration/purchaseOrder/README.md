# Migrate Purchase Order

This example migrates a legacy purchase order to Tradecloud One using the Migration API

## Prerequisites

Support powers

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend migratePurchaseOrderUrl if necessary

Amend purchaseOrderNumber.json if necessary:
- line status: `legacy_purchase_order_line_status_unconfirmed`, `legacy_purchase_order_line_status_inconsistent` or`legacy_purchase_order_line_status_confirmed`
- please note that all `...Confirmed` objects must be removed in case of `legacy_purchase_order_line_status_unconfirmed`
- please note that `agreed` or `approved` must be set to `true` in case of `legacy_purchase_order_line_status_confirmed`

## Run

```
➜  purchaseOrder git:(master) ✗ dotnet run
Tradecloud migrate purchase order example.
Login response StatusCode: 200 ElapsedMilliseconds: 550
Login response Content: {"username": ... }
MigratePurchaseOrder start=8/29/2022 9:17:58 PM elapsed=397ms status=200 reason=OK
MigratePurchaseOrder response body={
  "ok": true
}
```