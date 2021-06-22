# Send SCSN Order with embedded file using the Isah SCI Connector

This example sends a SCSN order containing an embedded Base64 encoded file to Tradecloud using the Isah SCI Connector

## Prerequisites

A Tradecloud user with `buyer` and `integration` roles

## Configure

In the source code:

- amend authenticationUrl if necessary
- fill in username on Tradecloud
- fill in password on Tradecloud
- amend sendOrderUrl if necessary
- amend file name if necessary

## Run

``` shell
➜  sci-connector git:(master) ✗ dotnet run
Tradecloud send order using Isah SCI Connector example.
SendOrder start: 1/14/2021 6:54:21 PM elapsed: 931 StatusCode: 200
SendOrder Body: {"ok":true}
```
