# Find account

This example finds an account based on companyId and account number

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- fill in the companyId in the findAccountByCodeUrl

## Run

``` shell
➜  FindCompanyById git:(master) ✗ dotnet run
Tradecloud find company by id example.
Login response StatusCode: 200 ElapsedMilliseconds: 408
Login response Content: ...
FindCompanyById start=12/24/2021 4:02:02 PM elapsed=22ms status=200 reason=OK
FindCompanyById response body={
  "id": ...
```
