# Find account

This example finds an account based on companyId and account number

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- fill in the companyId and accountCode in the findAccountByCodeUrl

## Run

``` shell
➜  FindAccountByCode git:(master) ✗ dotnet run
Tradecloud find account by code example.
Login response StatusCode: 200 ElapsedMilliseconds: 446
Login response Content: {"username":...}
FindAccountByCode start=10/6/2021 6:14:10 PM elapsed=54ms status=200 reason=OK
FindAccountByCode response body={
  "companyId": "...",
  "accountCode": "...",
  "accountCompanyId": "..."
}
```
