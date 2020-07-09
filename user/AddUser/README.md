# Add User

This example adds a user (without invite) to Tradecloud

## Prerequisites

Super powers

## Configure

In the source code:
- amend authenticationUrl
- fill in username
- fill in password
- add addUserUrl
- amend user fields

## Run

```
➜  AddUser git:(master) ✗ dotnet run
Tradecloud add user example.
Login response StatusCode: 200 ElapsedMilliseconds: 540
Login response Content: ...
AddUser StatusCode: 200 ElapsedMilliseconds: 164
AddUser Body: {"id": ...}
```