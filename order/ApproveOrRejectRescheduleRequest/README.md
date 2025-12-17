# Approve or Reject Shipment Reschedule Request

This example demonstrates how to approve or reject a shipment reschedule request initiated by a supplier for a specific order line.

## Endpoints

- Approve: `POST /api-connector/order/{orderId}/line/{position}/reschedule/approve`
- Reject: `POST /api-connector/order/{orderId}/line/{position}/reschedule/reject`

See the [order API specs](https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml)

## Configuration

Edit `ApproveOrRejectRescheduleRequest.cs` and set:
- `accessToken`: Your API access token
- `orderId`: The order ID
- `linePosition`: The order line position
- `action`: Set to `"approve"` or `"reject"`

## Request Body

### Approve Request (approve-request.json)
```json
{
  "version": 1
}
```

### Reject Request (reject-request.json)
```json
{
  "reason": "Rejected by the buyer",
  "version": 1
}
```

- `version`: Order version for optimistic locking (optional)
- `reason`: Rejection reason (required for reject, not applicable for approve)

## Run

```sh
dotnet run
```

