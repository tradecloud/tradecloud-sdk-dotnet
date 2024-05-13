# API v1 Acknowledge Order

This example acknowledges one fetched order on the API v1

The technical acknowledge means the order (update) has been received.

## Prerequisites

- A tradecloud.nl buyer or supplier user
- A fetched unacknowledged order

## Configure

In the source code:

- fill in your Tradecloud username
- fill in your password
- fill in the order id in the URL
- fill in the order versionHash in the body

## Run

```shell
➜  api-v1 git:(master) ✗ cd AcknowledgeOrder 
➜  AcknowledgeOrder git:(master) ✗ dotnet run
Tradecloud acknowledge order example.
AcknowledgeOrder status=200 reason=OK
AcknowledgeOrder response body={
  "ok": true
}
```
