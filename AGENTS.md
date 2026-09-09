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
- 16 top-level dirs (one per domain / connector / utility):
  `api-connector/`, `authentication/`, `company/`, `conversation/`,
  `forecast/`, `object-storage/`, `order/`, `order-line-search/`,
  `order-search/`, `order-webhook-connector/`, `sap-soap-connector/`,
  `sci-connector/`, `shipment/`, `shipment-webhook-connector/`,
  `user/`, `workflow/`
- Per-operation `README.md` files inside each domain dir document
  the sample's purpose

## Notes

- Mirrors the TC1 REST API surface; align changes with
  `tradecloud-docs-api-v2`.
- Sample credentials live in a per-project `.env` (gitignored). Copy
  `.env.template` to `.env` and fill in `TRADECLOUD_USERNAME` /
  `TRADECLOUD_PASSWORD` and/or `ACCESS_TOKEN`, depending on what the
  sample uses. Do not put secrets in source. Shared loader:
  `authentication/Authentication/EnvFile.cs`.
