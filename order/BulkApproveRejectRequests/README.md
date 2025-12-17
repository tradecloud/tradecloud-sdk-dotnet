# Bulk Approve or Reject Requests as Buyer

This example demonstrates how to bulk approve or reject order proposal, reopen, and reschedule requests initiated by supplier as a buyer.

## Endpoints

- Bulk approve: `POST /api-connector/order/{orderId}/requests/buyer/approve`
- Bulk reject: `POST /api-connector/order/{orderId}/requests/buyer/reject`

See the [order API specs](https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/order/private/specs.yaml)

## Configuration

Edit `BulkApproveRejectRequests.cs` and set:
- `accessToken`: Your API access token
- `orderId`: The order ID
- `action`: Set to `"approve"` or `"reject"`

## Request Body

### Approve Request (bulk-approve-request.json)
```json
{
  "linePositions": ["position1", "position2"],
  "version": 1
}
```

### Reject Request (bulk-reject-request.json)
```json
{
  "linePositions": ["position1", "position2"],
  "reason": "Rejected by the buyer",
  "version": 1
}
```

- `linePositions`: Array of order line positions to approve/reject (required)
- `version`: Order version for optimistic locking (optional)
- `reason`: Rejection reason (required for reject, not applicable for approve)

## Run

```sh
dotnet run
```

