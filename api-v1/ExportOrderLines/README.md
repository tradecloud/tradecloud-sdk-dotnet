# API v1 Export order lines

This example exports order lines from the API v1

- please note that only active (non-archived) order lines are exported, this is a limitation of the API
- status code 401 is returned when the authentication fails

## Prerequisites

- tradecloud.nl company user (not a support user)

## Configure

In the source code:

- fill in your tradecloud.nl username
- fill in your tradecloud.nl password
- fill in the environment (one of `accp` or `portal` for production)
- fill in the date from and date to
- fill in the file name to save the export to
 
## Run

``` shell
➜  api-v1 git:(master) ✗ cd ExportOrderLines 
➜  ExportOrderLines git:(master) ✗ dotnet run
Tradecloud export order lines example.
Reached end of pages at page 1
```
