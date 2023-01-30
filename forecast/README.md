# Search Orders

This example searches for forecasts

## Configure

In the source code:

- username on Tradecloud
- password on Tradecloud
- fill in the search query

## Run

``` shell
➜  SearchForecastLines git:(master) ✗ dotnet run
Tradecloud search forecast lines example.
Login response StatusCode: 200 ElapsedMilliseconds: 729
Login response Content: {...}
SearchForecastLines start=1/30/2023 9:43:54 PM elapsed=2753ms status=200 reason=OK
SearchForecastLines response body={
  "data": [...],
  ...
```
