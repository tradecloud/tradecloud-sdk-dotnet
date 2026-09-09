# Add Authorized Company

This example adds an authorized company to an integration user identity

## Prerequisites

Super powers

## Configure

In the source code:
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- fill in fields

## Run

```
➜  AddAuthorizedCompany git:(master) ✗ dotnet run
Tradecloud add authorized company example.
Login response StatusCode: 200 ElapsedMilliseconds: 383
Login response Content: {...}
AddAuthorizedCompany StatusCode: 200 ElapsedMilliseconds: 55
AddAuthorizedCompany Body: {"ok":true}
```