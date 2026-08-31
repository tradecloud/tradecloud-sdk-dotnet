# Test Login Header Case

Black-box test for login response header casing per HTTP version.

Login returns tokens in `Set-Authorization` and `Set-Refresh-Token`. HTTP/1.1 keeps the original capital letters (`preserveHeaderCase`, backwards compatible with clients that look up those names as documented). HTTP/2 and HTTP/3 require lowercase field names on the wire.

## What it tests

Logs in with Basic Auth on each protocol version and inspects the **received** header names (`HttpHeaders.NonValidated`), not a case-insensitive lookup.

| Version | Expected header names | Notes |
|---|---|---|
| HTTP/1.1 | `Set-Authorization`, `Set-Refresh-Token` | Preserved case |
| HTTP/2 | `set-authorization`, `set-refresh-token` | Lowercase (RFC 9113) |
| HTTP/3 | `set-authorization`, `set-refresh-token` | Lowercase (RFC 9114); skipped if the client or host has no HTTP/3 |

`.NET` `HttpClient.TryGetValues` is case-insensitive, so `Authentication.Login` still finds the tokens on HTTP/2 and HTTP/3. This test checks the wire names, not that lookup.

`HttpClient` rewrites well-known headers (`Date`, `Server`, `Cache-Control`) to a canonical form even on HTTP/2. The token headers are custom, so their names stay as received.

## Prerequisites

Fill in `username` and `password` in `TestLoginHeaderCase.cs`, or pass them as arguments.

HTTP/3 on Linux also needs [libmsquic](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/http3). If HTTP/3 cannot be negotiated, that case is skipped; HTTP/1.1 and HTTP/2 still run.

## Run

```shell
dotnet run
```

Or without editing the source:

```shell
dotnet run -- <username> <password>
```

Exit code `0` when HTTP/1.1 and HTTP/2 pass (HTTP/3 may be skipped). Exit code `1` when a required case fails.

## Expected output

```shell
=== Tradecloud login HTTP header case test ===

--- HTTP/1.1 ---
  ...
  Set-Authorization: 'Set-Authorization' (expected 'Set-Authorization') OK
  Set-Refresh-Token: 'Set-Refresh-Token' (expected 'Set-Refresh-Token') OK
  RESULT: PASS - HTTP/1.1 token headers use preserved capital letters (HTTP/1.1 preserveHeaderCase)

--- HTTP/2.0 ---
  ...
  Set-Authorization: 'set-authorization' (expected 'set-authorization') OK
  Set-Refresh-Token: 'set-refresh-token' (expected 'set-refresh-token') OK
  RESULT: PASS - HTTP/2.0 token headers use lowercase names (HTTP/2 and HTTP/3 wire format)
```
