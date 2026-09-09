# Update User

This example updates the user profile

## Configure

In the source code:
- amend authenticationUrl
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- add <userId> in updateUserUrl
- amend user.json

## Run

```
➜  UpdateUser git:(master) ✗ dotnet run
Tradecloud update user example.
Login response StatusCode: 200 ElapsedMilliseconds: 374
Login response Content: {"username": ... }
UpdateUser StatusCode: 200 ElapsedMilliseconds: 128
UpdateUser Body: {"ok":true}
```