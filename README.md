# Tradecloud .NET SDK

The .NET SDK can help you to develop a Tradecloud API v2 client and webhook service in .NET and C#

## Prerequisites

Download and install [.NET Core (either Runtime or SDK) 8.0](https://dotnet.microsoft.com/download/dotnet-core/8.0)

## Clone the repository

``` shell
git clone https://github.com/tradecloud/tradecloud-sdk-dotnet.git
cd tradecloud-sdk-dotnet
```

## Update the repository

``` shell
git fetch
git pull
```

## To refresh the solution file

```bash
dotnet sln add (ls -r **/*.csproj)
```

```zsh
dotnet sln add $(ls -r **/*.csproj)
```

## Credentials

Each sample that talks to the API has a `.env.template` next to the `.csproj`. Copy it to `.env` in the same folder and fill in the values it asks for. `.env` is gitignored.

| Sample uses | Variables |
| --- | --- |
| Login (username / password) | `TRADECLOUD_USERNAME`, `TRADECLOUD_PASSWORD` |
| Bearer token | `ACCESS_TOKEN` |
| Long-running token refresh | `ACCESS_TOKEN` and/or `REFRESH_TOKEN` |

Do not commit `.env` or put credentials in source.
