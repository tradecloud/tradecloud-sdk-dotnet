# Add User

This example adds a user (without invite) to Tradecloud

## Prerequisites

A Tradecloud super user

## Configure

In the source code:
- amend authenticationUrl if necessary
- fill in super user username
- fill in super user password
- amend addUserUrl in necessary
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