using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class TestImageUploadLimit
    {
        /// <summary>API root including <c>/v2</c> (e.g. accp or a feature host).</summary>
        const string baseUrl = "https://api.accp.tradecloud1.com/v2";

        const string authenticationUrl = baseUrl + "/authentication/";
        const string uploadImageUrl = baseUrl + "/object-storage/image";

        /// <summary>
        /// Image upload limit in bytes: 8 MiB = 8L * 1024 * 1024 = 8_388_608.
        /// Unlike the document route there is no DOCUMENT_SIZE_LIMIT equivalent and no
        /// withSizeLimit on this route, so the cap is the Pekko
        /// pekko.http.server.parsing.max-content-length default, which object-storage does
        /// not override. It is the same 8 MiB the published spec documents on the 413.
        /// Envoy has no request-size guard, so this service-side cap is the only one.
        /// </summary>
        static readonly long uploadLimitBytes = 8L * 1024 * 1024;

        /// <summary>How far under/over the configured limit to probe.</summary>
        static readonly long probeDeltaBytes = 1L * 1024 * 1024;

        /// <summary>
        /// Content type of the uploaded part. Nothing decodes the bytes: object-storage
        /// streams them straight to the images bucket and stores this string as-is, so a
        /// zero-filled payload is enough to probe the size boundary.
        /// </summary>
        const string imageContentType = "image/png";

        /// <summary>
        /// Longer than the accp object-storage Envoy requestTimeout (300s) so a gateway
        /// 504 is observed as HTTP 504, not a client-side cancel.
        /// </summary>
        static readonly TimeSpan httpTimeout = TimeSpan.FromMinutes(10);

        static async Task<int> Main()
        {
            Console.WriteLine("=== Tradecloud Object Storage Image Upload Limit Probe ===");
            Console.WriteLine();

            if (!EnvFile.TryUsernamePassword(out var username, out var password))
                return 1;

            if (uploadLimitBytes <= 0)
            {
                Console.WriteLine("ERROR: Set uploadLimitBytes in TestImageUploadLimit.cs to the env limit you want to probe.");
                Console.WriteLine("       Default: 8L * 1024 * 1024 (8 MiB, the Pekko max-content-length default).");
                return 1;
            }

            if (uploadLimitBytes <= probeDeltaBytes)
            {
                Console.WriteLine($"ERROR: uploadLimitBytes ({uploadLimitBytes}) must be greater than probeDeltaBytes ({probeDeltaBytes}).");
                return 1;
            }

            var under = uploadLimitBytes - probeDeltaBytes;
            var equal = uploadLimitBytes;
            var over = uploadLimitBytes + probeDeltaBytes;

            Console.WriteLine($"Target: {baseUrl}");
            Console.WriteLine($"Configured limit: {FormatBytes(uploadLimitBytes)}");
            Console.WriteLine($"Delta:            {FormatBytes(probeDeltaBytes)}");
            Console.WriteLine($"Probes:           under={FormatBytes(under)}  equal={FormatBytes(equal)}  over={FormatBytes(over)}");
            Console.WriteLine($"Client timeout:   {httpTimeout}");
            Console.WriteLine("Pass: HTTP 200 or 413. Fail: disconnect, timeout (incl. Envoy 504), or any other status.");
            Console.WriteLine();

            using var httpClient = new HttpClient { Timeout = httpTimeout };
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, _) = await authenticationClient.Login(username, password);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            Console.WriteLine("Authenticated successfully.");
            Console.WriteLine();

            var results = new[]
            {
                await Probe(httpClient, "under", under),
                await Probe(httpClient, "equal", equal),
                await Probe(httpClient, "over", over)
            };

            Console.WriteLine();
            Console.WriteLine("--- Summary ---");
            var failed = 0;
            foreach (var result in results)
            {
                Console.WriteLine($"  {result.Label,-5} file={FormatBytes(result.FileBytes),-22} {result.Outcome}");
                if (!result.Passed)
                    failed++;
            }

            Console.WriteLine();
            if (failed == 0)
            {
                Console.WriteLine("RESULT: all probes returned 200 or 413 (no disconnect or timeout).");
                return 0;
            }

            Console.WriteLine($"RESULT: {failed} probe(s) failed.");
            return 1;
        }

        static async Task<ProbeResult> Probe(HttpClient httpClient, string label, long fileBytes)
        {
            Console.WriteLine($"--- Probe {label}: file {FormatBytes(fileBytes)} ---");

            using var fileStream = new SizedStream(fileBytes);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageContentType);

            using var multipart = new MultipartFormDataContent();
            multipart.Add(streamContent, "file", $"image-upload-limit-{label}.png");

            var contentLength = multipart.Headers.ContentLength;
            if (contentLength.HasValue)
                Console.WriteLine($"  multipart Content-Length: {FormatBytes(contentLength.Value)} (file + wrapping)");

            var watch = Stopwatch.StartNew();
            try
            {
                using var response = await httpClient.PostAsync(uploadImageUrl, multipart);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"  status={statusCode} {response.ReasonPhrase} elapsed={watch.ElapsedMilliseconds}ms");
                if (!string.IsNullOrWhiteSpace(body))
                    Console.WriteLine($"  body={Truncate(body, 200)}");

                string outcome;
                bool passed;
                if (statusCode == 200 || statusCode == 413)
                {
                    passed = true;
                    outcome = $"PASS HTTP {statusCode}";
                }
                else if (statusCode == 504)
                {
                    passed = false;
                    outcome = "FAIL timeout HTTP 504 (Envoy/gateway requestTimeout or streamIdleTimeout)";
                }
                else
                {
                    passed = false;
                    outcome = $"FAIL unexpected HTTP {statusCode}";
                }
                Console.WriteLine($"  {outcome}");
                Console.WriteLine();
                return new ProbeResult(label, fileBytes, passed, outcome);
            }
            catch (TaskCanceledException ex)
            {
                watch.Stop();
                var outcome = $"FAIL timeout after {watch.ElapsedMilliseconds}ms: {ex.Message}";
                Console.WriteLine($"  {outcome}");
                Console.WriteLine();
                return new ProbeResult(label, fileBytes, passed: false, outcome);
            }
            catch (HttpRequestException ex)
            {
                watch.Stop();
                var detail = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                var outcome = $"FAIL disconnect after {watch.ElapsedMilliseconds}ms: {detail}";
                Console.WriteLine($"  {outcome}");
                Console.WriteLine();
                return new ProbeResult(label, fileBytes, passed: false, outcome);
            }
            catch (IOException ex)
            {
                watch.Stop();
                var outcome = $"FAIL disconnect after {watch.ElapsedMilliseconds}ms: {ex.Message}";
                Console.WriteLine($"  {outcome}");
                Console.WriteLine();
                return new ProbeResult(label, fileBytes, passed: false, outcome);
            }
        }

        static string FormatBytes(long bytes)
        {
            return $"{bytes:N0} bytes ({bytes / (1024d * 1024d):0.##} MiB)";
        }

        static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            var singleLine = value.Replace("\r", "").Replace("\n", " ");
            return singleLine.Length <= maxLength ? singleLine : singleLine.Substring(0, maxLength) + "...";
        }

        readonly struct ProbeResult
        {
            public ProbeResult(string label, long fileBytes, bool passed, string outcome)
            {
                Label = label;
                FileBytes = fileBytes;
                Passed = passed;
                Outcome = outcome;
            }

            public string Label { get; }
            public long FileBytes { get; }
            public bool Passed { get; }
            public string Outcome { get; }
        }

        /// <summary>Seekable stream of a fixed length filled with zeros. Avoids writing large temp files.</summary>
        sealed class SizedStream : Stream
        {
            readonly long length;
            long position;

            public SizedStream(long length)
            {
                if (length < 0)
                    throw new ArgumentOutOfRangeException(nameof(length));
                this.length = length;
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => length;
            public override long Position
            {
                get => position;
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var remaining = length - position;
                if (remaining <= 0)
                    return 0;
                var n = (int)Math.Min(count, remaining);
                Array.Clear(buffer, offset, n);
                position += n;
                return n;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long next = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => position + offset,
                    SeekOrigin.End => length + offset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin))
                };
                if (next < 0)
                    throw new IOException("An attempt was made to move the position before the beginning of the stream.");
                position = next;
                return position;
            }

            public override void Flush() { }

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

    }
}
