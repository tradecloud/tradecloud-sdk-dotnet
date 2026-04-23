# tradecloud-sdk-dotnet

## Purpose

.NET SDK for the TC1 API v2. One top-level dir per domain entity
or connector, each containing per-operation .csproj samples
(SendOrder, AttachOrderDocuments, FindIdentityByEmail, etc.).

## Module / Stack

C# / .NET 8.0. Single solution: `tradecloud-sdk-dotnet.sln`.
No `Makefile`; refresh the solution with
`dotnet sln add $(ls -r **/*.csproj)`.

## Build

`dotnet build` / `dotnet test` against the solution.

## Entry Points

- `tradecloud-sdk-dotnet.sln` -- solution
- 17 top-level dirs (one per domain / connector / utility):
  `api-connector/`, `authentication/`, `company/`, `conversation/`,
  `forecast/`, `migration/`, `object-storage/`, `order/`,
  `order-line-search/`, `order-search/`, `order-webhook-connector/`,
  `sap-soap-connector/`, `sci-connector/`, `shipment/`,
  `shipment-webhook-connector/`, `user/`, `workflow/`
- `migration/` -- legacy SDK migration helpers
- Per-operation `README.md` files inside each domain dir document
  the sample's purpose

## Notes

- Mirrors the TC1 REST API surface; align changes with
  `tradecloud-docs-api-v2`.
