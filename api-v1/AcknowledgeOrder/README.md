# API v1 Acknowledge Order

This example acknowledges one fetched order on the API v1

## Prerequisites

A tradecloud.nl user
A fetched unacknowledged order

## Configure

In the source code:
- fill in username on tradecloud.nl
- fill in password on tradecloud.nl
- fill in the order id in the URL
- fill in the order versionHash in the body
## Run

```
➜  AcknowledgeOrder git:(master) ✗ dotnet run
Tradecloud acknowledge order example.
AcknowledgeOrder status=200 reason=OK
AcknowledgeOrder response body={
  "ok": true
}
```