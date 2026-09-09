# Send Despatch Advice

This example sends a despatch advice to Tradecloud using the API Connector

## Prerequisites

A Tradecloud user with `supplier` and `integration` roles

## Configure

In the source code:

- amend authenticationUrl if necessary
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- amend sendDespatchAdviceUrl if necessary

Amend despatch-advice.json if necessary:

- amend the `purchaseOrderNumber`

## Run

```
➜  SendDespatchAdvice git:(master) ✗ dotnet run
Tradecloud send despatch advice by supplier example.
SendDespatchAdvice start=12/6/2021 7:02:00 PM elapsed=776ms status=200 reason=OK
SendDespatchAdvice response body={
  "ok": true
}
```