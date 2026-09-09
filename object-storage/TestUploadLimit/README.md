# Test Upload Limit

Black-box probe for the object-storage upload size limit of a configured environment.

Uploads a generated payload a bit under, exactly equal to, and a bit over a **configured** limit. Each probe must complete with HTTP **200** or **413**. A disconnect or timeout is a failure: the env should reject oversize bodies cleanly, not drop the connection.

This is the check [TC-11170](https://tradecloud.atlassian.net/browse/TC-11170) called out for accp after the Envoy Gateway cutover: nginx `proxy-body-size` is gone, and there is no Envoy request-size equivalent.

## What it tests

Three `POST /v2/object-storage/document` uploads of generated zero-filled files:

| Probe | File size |
| --- | --- |
| under | `uploadLimitBytes - 1 MiB` |
| equal | `uploadLimitBytes` |
| over | `uploadLimitBytes + 1 MiB` |

- **PASS**: HTTP 200 (accepted) or 413 (payload too large)
- **FAIL**: timeout (client cancel, or HTTP 504 from Envoy), connection reset / disconnect, or any other status

The probe sizes the **file** part. Multipart wrapping adds a few hundred bytes on top; that Content-Length is printed per probe.

Typical outcomes when the configured value matches the env:

- under: 200
- equal: 200 or 413 (413 if the limit applies to the whole HTTP body, because of multipart wrapping)
- over: 413

A disconnect on the oversize probe is the Envoy failure mode this exists to catch. Rejecting on `Content-Length` at the edge (the old `httpRoute.maxRequestBytes` Lua guard) closed the connection with unread body data, the TCP stack sent RST, and the client never saw the 413. That guard was removed (`internal-helm-charts` #168). Oversize must be rejected by object-storage (`withSizeLimit` / `DOCUMENT_SIZE_LIMIT`), which can drain the body and return 413.

## Configure

Copy `.env.template` to `.env` in this folder and set:

```
TRADECLOUD_USERNAME=you@example.com
TRADECLOUD_PASSWORD=...
```

`.env` is gitignored. Then in `TestUploadLimit.cs`:

- set `uploadLimitBytes` to the limit you want to discover (must be greater than 0)
- amend `baseUrl` if necessary

Known env limits (set `uploadLimitBytes` to the one you are probing):

| Env | Limit | Bytes |
| --- | --- | --- |
| Accp and other non-prod | 256 MiB | `256L * 1024 * 1024` = 268_435_456 |
| Prod | 512 MiB | `512L * 1024 * 1024` = 536_870_912 |

On accp, Envoy has no request-size guard, so this must match object-storage `DOCUMENT_SIZE_LIMIT`. Accp object-storage `BackendTrafficPolicy` is `requestTimeout` 300s / `streamIdleTimeout` 60s.

The default in `TestUploadLimit.cs` is 256 MiB (accp). Switch to `512L * 1024 * 1024` to probe prod.

The HTTP client timeout is 10 minutes, longer than the 300s Envoy object-storage `requestTimeout`, so a gateway timeout shows up as HTTP 504 rather than a client cancel.

## Run

```shell
cp .env.template .env
# edit .env
dotnet run
```

## Expected output

```shell
=== Tradecloud Object Storage Upload Limit Probe ===

Configured limit: 268,435,456 bytes (256 MiB)
Probes:           under=...  equal=...  over=...

Authenticated successfully.

--- Probe under: file ... ---
  status=200 OK elapsed=...ms
  PASS HTTP 200

--- Probe equal: file ... ---
  status=200 OK elapsed=...ms
  PASS HTTP 200

--- Probe over: file ... ---
  status=413 Payload Too Large elapsed=...ms
  PASS HTTP 413

--- Summary ---
  under ... PASS HTTP 200
  equal ... PASS HTTP 200
  over  ... PASS HTTP 413

RESULT: all probes returned 200 or 413 (no disconnect or timeout).
```
