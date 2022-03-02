# Search users

This example search users by companyId

## Configure

In the source code:

- set username on Tradecloud
- set password on Tradecloud
- set companyId or other filters

## Run

```
➜  SearchUsers git:(master) ✗ dotnet run
Tradecloud search users example.
Login response StatusCode: 200 ElapsedMilliseconds: 417
Login response Content: {"username": ...}
SearchUsers StatusCode: 200 ElapsedMilliseconds: 18
SearchUsers Body: {"data":[{"id": ..."
```
