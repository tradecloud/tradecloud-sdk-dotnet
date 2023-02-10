# API v1 Resend Orders

This code bulk resends orders to your ERP using the API v1 or connector in use.

## Prerequisites

A tradecloud.nl user

## Configure

In the source code:
- fill in your Tradecloud username
- fill in your password
- provide orders.txt with an order code on each line

## Run

```
➜  api-v1 git:(master) ✗ cd ResendOrders 
➜  ResendOrders git:(master) ✗ dotnet run
Tradecloud resend orders.
resendOrder code=order_code1 id=order-id-1 status=200 reason=OK
  ...
```
