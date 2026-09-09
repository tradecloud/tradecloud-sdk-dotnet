# Add User

This example adds a user (without invite) to Tradecloud

## Prerequisites

Super powers

## Configure

In the source code:
- amend `addUserUrl`
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`

Amend `user.json`

## Run

```
➜  AddUser git:(master) ✗ dotnet run
Tradecloud add user example.
AddUser StatusCode: 200 ElapsedMilliseconds: 164
AddUser Body: {"id": ...}
```