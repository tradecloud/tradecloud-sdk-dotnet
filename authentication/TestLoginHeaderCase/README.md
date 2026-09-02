# Test Login Header Case

Black-box test for login response header casing per HTTP version, and for applying the received access token with Authorization header casing variants.

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

When an `orderId` is configured, the test then `GET /v2/order/{orderId}` using the access token from the login checks above, so a correctly cased request returns `200`. The token is taken from the last login that produced one, because an earlier login may already be superseded and a stale token would look like a rejected casing.

Each case changes only the casing of the `Authorization` header name and/or the `Bearer` scheme, and every case is run on both HTTP/1.1 and HTTP/2. Lowercase is the only field name casing HTTP/2 allows, so the scheme variants are tested under the lowercase name; the other name casings only need one scheme to show whether the name survives:

| Header name | Scheme | Varies |
|---|---|---|
| `authorization` | `Bearer` | scheme (documented casing) |
| `authorization` | `bearer` | scheme |
| `authorization` | `BEARER` | scheme |
| `authorization` | `BeArEr` | scheme (mixed) |
| `Authorization` | `bearer` | name |
| `AUTHORIZATION` | `bearer` | name |

These probes do **not** use `HttpClient`, because it cannot put a chosen header name casing on the wire: it maps the header onto the known `Authorization` header and writes the canonical name, and its HTTP/2 writer lowercases every field name. So every name variant would collapse into the same request.

`RawHttpClient` (in this project) writes the request bytes itself, so the name reaches the wire exactly as given:

- **HTTP/1.1** -- opens a TLS connection with ALPN `http/1.1` and writes the request line and header lines verbatim.
- **HTTP/2** -- opens a TLS connection with ALPN `h2`, writes the connection preface, a `SETTINGS` frame, and a `HEADERS` frame whose HPACK block encodes each probe header as a literal with a new, non-Huffman name. The pseudo-headers use the HPACK static table. Only `:status` is decoded from the response, which needs no dynamic table and no full Huffman table.

Uppercase field names are malformed for HTTP/2 (RFC 9113 section 8.2), so a reject there is the correct server behaviour, not a defect. The report therefore records the observed outcome instead of only pass or fail, and groups the cases by outcome:

| Outcome | Meaning |
|---|---|
| `ACCEPTED` | `200`, the casing was honoured |
| `MALFORMED` | `400`, an HTTP/2 `RST_STREAM` or `GOAWAY`, or the connection closed without a response, so the request never reached authorization |
| `REJECTED` | `401` or `403`, the casing was not honoured |
| `UNEXPECTED` | any other status, for example `404` for an unknown order id |
| `ERROR` | transport, TLS, or ALPN failure, so nothing was measured |

Groups are printed in that order, and an empty group is left out.

## Prerequisites

Fill in `username` and `password` in `TestLoginHeaderCase.cs`, or pass them as arguments.

Fill in `orderId` in the same file, or pass it as the third argument, to run the Authorization casing probes. Without an order id those cases are skipped.

HTTP/3 needs a QUIC listener on the server and QUIC support in the client; on Linux .NET also needs [libmsquic](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/http3). If HTTP/3 cannot be negotiated, that case is skipped and the test prints which side is missing it: a server offering HTTP/3 advertises it in `Alt-Svc`, so the absence of that header points at the server.

At the time of writing, `api.accp.tradecloud1.com` does not offer HTTP/3. It sends no `Alt-Svc` header, UDP port 443 refuses connections (`curl --http3-only` fails), and it is served by `pekko-http`, which supports HTTP/1.1 and HTTP/2 only. So the HTTP/3 skip is expected and not a client configuration problem.

## Run

```shell
dotnet run
```

Or without editing the source:

```shell
dotnet run -- <username> <password> [orderId]
```

Exit code `0` when the HTTP/1.1 and HTTP/2 login checks pass (HTTP/3 may be skipped), the casing a compliant client would send (`authorization: Bearer`, valid on every version) is `ACCEPTED`, and no probe ended in `ERROR`. The casing variants are reported, not failed. Exit code `1` otherwise.

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

=== Authorization header case (GET /v2/order/{orderId}) ===

  Access token: from the HTTP/2 login above (812 chars)

--- HTTP/1.1 authorization: Bearer <access-token> ---
  GET https://api.accp.tradecloud1.com/v2/order/<orderId>
  Wire header: 'authorization: Bearer <access-token>'
  Status: 200
  RESULT: ACCEPTED - 200

--- HTTP/2.0 AUTHORIZATION: bearer <access-token> ---
  GET https://api.accp.tradecloud1.com/v2/order/<orderId>
  Wire header: 'AUTHORIZATION: bearer <access-token>'
  No status: closed without response
  RESULT: MALFORMED - closed without response

=== Summary ===

Login: token response header names
  HTTP/1.1  Set-Authorization, Set-Refresh-Token    PASSED
  HTTP/2    set-authorization, set-refresh-token    PASSED
  HTTP/3    set-authorization, set-refresh-token    SKIPPED

(the HTTP/3 block above states why, for example:
  SKIP: HTTP/3 is not available (HttpRequestException: ...)
  WHY:  server side: no Alt-Svc header on the HTTP/1.1 or HTTP/2 response, so the API does not offer HTTP/3)

GET /v2/order/{orderId}: Authorization + Bearer casing, sent as raw wire bytes

  ACCEPTED -- 200, the casing was honoured
    HTTP/1.1 authorization: Bearer      200
    HTTP/1.1 authorization: bearer      200
    HTTP/1.1 authorization: BEARER      200
    HTTP/1.1 authorization: BeArEr      200
    HTTP/1.1 Authorization: bearer      200
    HTTP/1.1 AUTHORIZATION: bearer      200
    HTTP/2.0 authorization: Bearer      200
    HTTP/2.0 authorization: bearer      200
    HTTP/2.0 authorization: BEARER      200
    HTTP/2.0 authorization: BeArEr      200

  MALFORMED -- 400 or a refused HTTP/2 header block, the request never reached authorization
    HTTP/2.0 Authorization: bearer      closed without response
    HTTP/2.0 AUTHORIZATION: bearer      closed without response
```

Note that on HTTP/2 only the lowercase `authorization` name reaches the service; the capitalised names are refused before any application code sees them. The `Bearer` scheme casing is not part of the field name, so it is delivered as written on both versions.

