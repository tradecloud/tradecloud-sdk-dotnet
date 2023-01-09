# API v1 Archive Order Line

This example archives one order line on the API v1

## Prerequisites

- A tradecloud.nl supplier user
- An order line to archive

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password
- fill in the order LINE id in the URL
## Run

```
➜  ArchiveOrderLine git:(master) ✗ dotnet run
Tradecloud archive order line example.
ArchiveOrderLine status=200 reason=OK
ArchiveOrderLine response body={
  ...
```
