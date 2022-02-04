# Order and line status fields

Tradecloud orders contain a status field on order and on line level:

- Buyer action: the required action by the buyer in case of this status.
- Supplier action: the required action by the supplier in case of this status.

Exceptions, like `inconsistent`, `rejected`, `overdue` and `cancelled` status, can be discussed between buyer and supplier in the Tradecloud portal.

## Order status

| Status  |  Description | Buyer action | Supplier action |
|---|---|---|---|
| `open` | One or more lines are unconfirmed or reopened | No action required | Please confirm the unconfirmed or reopened lines |
| `inconsistent` | One or more lines are inconsistent confirmed by supplier | Approve or reject the inconsistent lines | No action required |
| `rejected` | One or more lines are rejected by buyer | No action required | Reconfirm the rejected lines |
| `approved` | All lines are approved by buyer | No action required | Deliver the order to the buyer as agreed |
| `confirmed` | All lines are confirmed by supplier or approved by buyers | No action required | Deliver the order to the buyer as agreed |
| `overdue` | One or more lines are overdue | No action required | Reconfirm and deliver the overdue lines |
| `shipped` | All lines are being shipped to buyer | No action required | No action required |
| `delivered` | All lines are delivered at buyer  | No action required | No action required |
| `cancelled` | One or more lines are cancelled by buyer | No action required | Stop delivery of the cancelled lines |
  

## Line status

| Status | Description | Buyer action | Supplier action |
|---|---|---|---|
| `unconfirmed` | The line is added by buyer and unconfirmed | No action required | Please confirm the unconfirmed line |
| `reopened` | The line was previously agreed, but is reopened by buyer | No action required | Reconfirm the reopened line |
| `inconsistent` | The line is confirmed inconsistent by supplier | Approve or reject the inconsistent line | No action required | No action required |  
| `rejected` | The inconsistent line is rejected by buyer | No action required | Reconfirm the rejected line |
| `approved` | The inconsistent line is approved by buyer | No action required | Deliver the line to the buyer as agreed |
| `confirmed` | The line is confirmed by supplier | No action required | Deliver the line to the buyer as agreed |
| `overdue` | The line is overdue | No action required | Reconfirm and deliver the overdue line |
| `shipped` | The line is shipped to buyer | No action required |  No action required |
| `delivered` | The line is delivered at buyer | No action required |  No action required |
| `cancelled` | The line is cancelled by buyer | No action required | Stop delivery of the cancelled lines |
  