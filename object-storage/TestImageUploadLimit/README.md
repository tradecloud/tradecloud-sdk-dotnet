# Test Image Upload Limit

Black-box probe for the object-storage **image** upload size limit of a configured environment. For the document endpoint see [TestDocUploadLimit](../TestDocUploadLimit/README.md).

Uploads a generated payload a bit under, exactly equal to, and a bit over the limit against [`uploadImage`](https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/private/specs.yaml#/object-storage/uploadImage). Each probe must complete with HTTP **200** or **413**. A disconnect or timeout is a failure: the env should reject oversize bodies cleanly, not drop the connection.

## What it tests

Three `POST /v2/object-storage/image` uploads of generated zero-filled files:

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

## Where the image limit comes from

8 MiB (`8L * 1024 * 1024` = 8_388_608), the same value the spec documents on the 413 response.

The image route differs from the document route in how that limit is enforced, which is why this probe exists separately:

- The document route applies `withSizeLimit(documentSizeLimit)` and, on an oversize declared Content-Length, **drains** the body before returning 413.
- `uploadImageRoute` in `ObjectStorageRoutes.scala` applies neither. Its only cap is the Pekko `pekko.http.server.parsing.max-content-length` default of 8 MiB, which object-storage does not override. The parser raises `EntityStreamSizeException` mid-stream and `fileUploadExceptionHandler` completes with `ContentTooLarge`.

Rejecting mid-stream with unread body data is exactly the shape that produced a TCP RST rather than a delivered 413 on the document route (the removed Envoy `httpRoute.maxRequestBytes` Lua guard, `internal-helm-charts` #168). There is no drain on the image path, so whether the oversize probe actually receives its 413 is the thing being measured. Envoy has no request-size guard of its own, so nothing rejects earlier at the edge.

`parseFileMultipart` also sets `withRequestTimeout(60.seconds)` on this route.

Nothing decodes the uploaded bytes: object-storage streams them to the images bucket and stores the declared content type as-is, so a zero-filled `image/png` part is a valid probe.

## Configure

Copy `.env.template` to `.env` in this folder and set:

```
TRADECLOUD_USERNAME=you@example.com
TRADECLOUD_PASSWORD=...
```

`.env` is gitignored. Then in `TestImageUploadLimit.cs`:

- amend `baseUrl` if necessary
- amend `uploadLimitBytes` only if the env overrides `pekko.http.server.parsing.max-content-length`

The HTTP client timeout is 10 minutes, longer than the 300s Envoy object-storage `requestTimeout`, so a gateway timeout shows up as HTTP 504 rather than a client cancel.

## Run

```shell
cp .env.template .env
# edit .env
dotnet run
```

## Expected output

```shell
=== Tradecloud Object Storage Image Upload Limit Probe ===

Configured limit: 8,388,608 bytes (8 MiB)
Probes:           under=...  equal=...  over=...

Authenticated successfully.

--- Probe under: file ... ---
  status=200 OK elapsed=...ms
  PASS HTTP 200

--- Probe equal: file ... ---
  status=413 Payload Too Large elapsed=...ms
  PASS HTTP 413

--- Probe over: file ... ---
  status=413 Payload Too Large elapsed=...ms
  PASS HTTP 413

--- Summary ---
  under ... PASS HTTP 200
  equal ... PASS HTTP 413
  over  ... PASS HTTP 413

RESULT: all probes returned 200 or 413 (no disconnect or timeout).
```
