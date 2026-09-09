# Send Order Batch

This example sends a batch of parallel orders to Tradecloud for adhoc performance testing

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:
- amend authenticationUrl if necessary
- copy `.env.template` to `.env` and fill in `ACCESS_TOKEN`
- amend sendOrderUrl in necessary

## Run

```
➜  SendOrderBatch git:(master) ✗ dotnet run
Tradecloud send order batch.
Login response StatusCode: 200 ElapsedMilliseconds: 1128
Login response Content: {"username":"agrifac-integration@tradecloud1.com"...
SendOrderBatch done, Count: 500 ElapsedMilliseconds: 4624
```