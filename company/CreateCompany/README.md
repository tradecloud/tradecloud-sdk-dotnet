# Add User

This example creates a company in Tradecloud

## Prerequisites

Super powers

## Configure

In the source code:

- amend authenticationUrl
- copy `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` and `TRADECLOUD_PASSWORD`
- amend `company.json`

## Run

``` shell
➜  CreateCompany git:(master) ✗ dotnet run
Tradecloud add company example.
Login response StatusCode: 200 ElapsedMilliseconds: 540
Login response Content: ...
CreateCompany StatusCode: 200 ElapsedMilliseconds: 164
CreateCompany Body: {"id": ...}
```