# Add Migration Whitelists

This example adds migration whitelists using the Migration API

## Prerequisites

Support powers

## Configure

In the source code:
- amend authenticationUrl if necessary
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- amend migrationWhitelistUrl if necessary
- amend whitelists.json

## Run

```
➜  whitelists git:(master) ✗ dotnet run
Tradecloud migration whitelist example.
Login response StatusCode: 200 ElapsedMilliseconds: 550
Login response Content: {"username": ... }
AddWhitelists start=11/11/2022 1:57:15 PM elapsed=36ms status=200 reason=OK
```