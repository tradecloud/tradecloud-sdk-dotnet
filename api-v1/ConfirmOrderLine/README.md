# API v1 Confirm Order Line

This example confirms one order line on the API v1

The business confirmation of the requested values, like quantity, delivery date and prices.

The confirmation can either be consistent (confirmed values are equal to requested prices) or inconsistent (at least one confirmed values is different than the requested values).

## Prerequisites

- A tradecloud.nl supplier user
- A to be confirmed order line

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password
- fill in the order LINE id in the URL
- fill in the to be confirmed values in the body
## Run

```
➜  api-v1 git:(master) ✗ cd ConfirmOrderLine 
➜  ConfirmOrderLine git:(master) ✗ dotnet run
Tradecloud confirm order line example.
ConfirmOrderLine status=200 reason=OK
ConfirmOrderLine response body={
  ...
```
