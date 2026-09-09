# Add User

This example adds an identity to Tradecloud

## Prerequisites

Super powers

## Configure

In the source code:
- amend `addIdentityUrl`
- copy `.env.template` to `.env` and fill in `ACCESS_TOKEN`

Amend `identity.json`

## Run

```
➜  AddIdentity git:(master) ✗ dotnet run
Tradecloud add identity example.
AddIdentity StatusCode: 200 ElapsedMilliseconds: 361
AddIdentity Body: ...
```