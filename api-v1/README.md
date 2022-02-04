# Tradecloud API v1 .NET SDK

The .NET SDK can help you to develop a Tradecloud API v1 client in .NET and C#

## Prerequisites

[.NET Core (Runtime or SDK) 5.0](https://dotnet.microsoft.com/download/dotnet-core/5.0)

## Clone

```
➜ git clone https://github.com/tradecloud/tradecloud-sdk-dotnet.git
➜ cd tradecloud-sdk-dotnet/api-v1
```

## Update

```
➜ git fetch
➜ git pull
```

## Flows

### Buyer 

1. [Send new or updated order](https://github.com/tradecloud/tradecloud-sdk-dotnet/tree/master/api-v1/SendOrder)

2. [Fetch confirmed orders](https://github.com/tradecloud/tradecloud-sdk-dotnet/tree/master/api-v1/GetUnacknowledgedOrders)

3. [Acknowledge fetched orders](https://github.com/tradecloud/tradecloud-sdk-dotnet/tree/master/api-v1/AcknowledgeOrder)

### Supplier

1. [Fetch new or updated orders](https://github.com/tradecloud/tradecloud-sdk-dotnet/tree/master/api-v1/GetUnacknowledgedOrders)

2. [Acknowledge fetched orders](https://github.com/tradecloud/tradecloud-sdk-dotnet/tree/master/api-v1/AcknowledgeOrder)

3. [Confirm order lines](https://github.com/tradecloud/tradecloud-sdk-dotnet/tree/master/api-v1/ConfirmOrderLine)

## Order and line status fields

[Order and line status fields](status.md)
