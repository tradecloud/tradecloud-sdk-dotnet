# API v1 Resend Orders

This example resends one order to your ERP using the API v1 or connector in use

## Prerequisites

A tradecloud.nl user

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password
- fill in purchaseOrderCode

## Run

```
➜  api-v1 git:(master) ✗ cd ResendOrder
➜  ResendOrder git:(master) ✗ dotnet run
Tradecloud resend order.
ResendOrder code=order_code1 id=order-id-1 status=200 reason=OK
```
