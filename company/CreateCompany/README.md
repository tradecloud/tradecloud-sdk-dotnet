# Add User

This example creates a company in Tradecloud

## Prerequisites

Super powers

## Configure

In the source code:
- amend authenticationUrl
- fill in username
- fill in password
- amend company fields

## Run

```
➜  CreateCompany git:(master) ✗ dotnet run
Tradecloud add company example.
Login response StatusCode: 200 ElapsedMilliseconds: 540
Login response Content: ...
CreateCompany StatusCode: 200 ElapsedMilliseconds: 164
CreateCompany Body: {"id": ...}
```