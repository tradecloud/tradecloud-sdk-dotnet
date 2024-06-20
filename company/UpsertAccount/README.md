# Add User

This example upserts a company's account in Tradecloud

## Prerequisites

Admin role

## Configure

In the source code:

- amend authenticationUrl
- fill in username
- fill in password
- amend `account.json`

## Run

``` shell
➜  UpsertAccount git:(master) ✗ dotnet run
Tradecloud add company example.
Login response StatusCode: 200 ElapsedMilliseconds: 540
Login response Content: ...
UpsertAccount StatusCode: 200 ElapsedMilliseconds: 164
UpsertAccount Body:...
```
