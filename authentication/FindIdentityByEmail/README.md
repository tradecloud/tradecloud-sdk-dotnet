# Find identity based on email

This example finds an identity based on email in the authentication service

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- user email

## Run

``` json
➜  FindIdentityByEmail git:(master) ✗ dotnet run
Tradecloud find identity by email example.
Login response StatusCode: 200 ElapsedMilliseconds: 423
Login response Content: {"username": ...
FindIdentityByEmail StatusCode: 200 ElapsedMilliseconds: 30
FindIdentityByEmail Body: {
  "username": ...
```
