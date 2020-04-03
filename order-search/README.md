# Get order by id

This example gets an order by Tradecloud id from the order-search service

## Configure

In the source code:
- username on Tradecloud
- password on Tradecloud
- orderId on Tradecloud

## Run

```
➜  order-search git:(master) ✗ dotnet run
Tradecloud get order by id example.
Authenticate StatusCode: 200
Authenticate Content: OK
GetOrderById StatusCode: 200
GetOrderById Content: {
  "id": "...",
  ...
```