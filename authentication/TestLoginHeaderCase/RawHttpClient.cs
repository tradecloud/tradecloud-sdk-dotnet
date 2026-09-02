using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    // Minimal GET client that writes request header bytes verbatim.
    //
    // HttpClient cannot send a chosen Authorization header name casing: it maps the header onto
    // the known Authorization header and writes the canonical name, and its HTTP/2 writer
    // lowercases every field name. This client encodes the header block itself, so the name
    // reaches the wire exactly as given -- including names that are malformed for HTTP/2.
    class RawHttpClient
    {
        const int frameHeaderLength = 9;

        const byte frameData = 0x0;
        const byte frameHeaders = 0x1;
        const byte frameRstStream = 0x3;
        const byte frameSettings = 0x4;
        const byte framePing = 0x6;
        const byte frameGoAway = 0x7;

        const byte flagAck = 0x1;
        const byte flagEndStream = 0x1;
        const byte flagEndHeaders = 0x4;

        // HPACK static table indexes (RFC 7541 appendix A).
        const int indexAuthority = 1;
        const int indexMethodGet = 2;
        const int indexPathSlash = 4;
        const int indexSchemeHttps = 7;
        const int indexStatus = 8;

        static readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

        public static async Task<RawHttpResponse> GetHttp11Async(
            Uri uri,
            IReadOnlyList<(string Name, string Value)> headers)
        {
            using var cancellation = new CancellationTokenSource(timeout);
            try
            {
                using var connection = await ConnectAsync(uri, SslApplicationProtocol.Http11, cancellation.Token);
                var negotiated = connection.Protocol.Protocol.Length;
                if (negotiated != 0 && connection.Protocol != SslApplicationProtocol.Http11)
                    return RawHttpResponse.Unavailable($"server negotiated '{connection.Protocol}' instead of http/1.1");

                var request = BuildHttp11Request(uri, headers);
                await connection.Stream.WriteAsync(request, cancellation.Token);
                await connection.Stream.FlushAsync(cancellation.Token);

                var raw = await ReadToEndAsync(connection.Stream, cancellation.Token);
                return ParseHttp11Response(raw);
            }
            catch (Exception ex)
            {
                return RawHttpResponse.Unavailable(Describe(ex));
            }
        }

        public static async Task<RawHttpResponse> GetHttp2Async(
            Uri uri,
            IReadOnlyList<(string Name, string Value)> headers)
        {
            using var cancellation = new CancellationTokenSource(timeout);
            try
            {
                using var connection = await ConnectAsync(uri, SslApplicationProtocol.Http2, cancellation.Token);
                if (connection.Protocol != SslApplicationProtocol.Http2)
                    return RawHttpResponse.Unavailable($"server did not negotiate h2 over ALPN (got '{connection.Protocol}')");

                var stream = connection.Stream;
                await stream.WriteAsync(Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"), cancellation.Token);
                await WriteFrameAsync(stream, frameSettings, 0, 0, Array.Empty<byte>(), cancellation.Token);
                await WriteFrameAsync(
                    stream,
                    frameHeaders,
                    flagEndHeaders | flagEndStream,
                    1,
                    EncodeHeaderBlock(uri, headers),
                    cancellation.Token);
                await stream.FlushAsync(cancellation.Token);

                return await ReadHttp2ResponseAsync(stream, cancellation.Token);
            }
            catch (Exception ex)
            {
                return RawHttpResponse.Unavailable(Describe(ex));
            }
        }

        static async Task<Connection> ConnectAsync(Uri uri, SslApplicationProtocol protocol, CancellationToken cancellation)
        {
            var tcp = new TcpClient();
            try
            {
                await tcp.ConnectAsync(uri.Host, uri.Port, cancellation);
                var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false);
                await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = uri.Host,
                        ApplicationProtocols = new List<SslApplicationProtocol> { protocol }
                    },
                    cancellation);
                return new Connection(tcp, ssl);
            }
            catch
            {
                tcp.Dispose();
                throw;
            }
        }

        static byte[] BuildHttp11Request(Uri uri, IReadOnlyList<(string Name, string Value)> headers)
        {
            var request = new StringBuilder();
            request.Append($"GET {uri.PathAndQuery} HTTP/1.1\r\n");
            request.Append($"Host: {uri.Authority}\r\n");
            foreach (var (name, value) in headers)
                request.Append($"{name}: {value}\r\n");
            request.Append("Accept: application/json\r\n");
            request.Append("Connection: close\r\n");
            request.Append("\r\n");
            return Encoding.ASCII.GetBytes(request.ToString());
        }

        static RawHttpResponse ParseHttp11Response(byte[] raw)
        {
            var boundary = IndexOfHeaderEnd(raw);
            if (boundary < 0)
                return RawHttpResponse.Unavailable("response without a complete header section");

            var lines = Encoding.ASCII.GetString(raw, 0, boundary).Split("\r\n");
            var headers = new List<(string Name, string Value)>();
            for (var i = 1; i < lines.Length; i++)
            {
                var separator = lines[i].IndexOf(':');
                if (separator <= 0)
                    continue;

                headers.Add((lines[i].Substring(0, separator), lines[i].Substring(separator + 1).Trim()));
            }

            var body = raw[(boundary + 4)..];
            return RawHttpResponse.Http(
                ParseStatusLine(lines[0]),
                headers,
                IsChunked(headers) ? Dechunk(body) : Encoding.UTF8.GetString(body));
        }

        static int ParseStatusLine(string statusLine)
        {
            var parts = statusLine.Split(' ');
            return parts.Length >= 2 && int.TryParse(parts[1], out var status) ? status : 0;
        }

        static bool IsChunked(List<(string Name, string Value)> headers)
        {
            foreach (var (name, value) in headers)
            {
                if (string.Equals(name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
                    && value.Contains("chunked", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static string Dechunk(byte[] body)
        {
            var text = Encoding.UTF8.GetString(body);
            var decoded = new StringBuilder();
            var index = 0;
            while (index < text.Length)
            {
                var lineEnd = text.IndexOf("\r\n", index, StringComparison.Ordinal);
                if (lineEnd < 0)
                    break;

                var sizeText = text.Substring(index, lineEnd - index).Split(';')[0].Trim();
                if (!int.TryParse(sizeText, System.Globalization.NumberStyles.HexNumber, null, out var size) || size == 0)
                    break;

                var start = lineEnd + 2;
                if (start + size > text.Length)
                    break;

                decoded.Append(text, start, size);
                index = start + size + 2;
            }

            return decoded.ToString();
        }

        static byte[] EncodeHeaderBlock(Uri uri, IReadOnlyList<(string Name, string Value)> headers)
        {
            var block = new List<byte>();
            EncodeIndexed(block, indexMethodGet);
            EncodeIndexed(block, indexSchemeHttps);
            EncodeLiteralWithIndexedName(block, indexAuthority, uri.Authority);
            EncodeLiteralWithIndexedName(block, indexPathSlash, uri.PathAndQuery);

            // A literal with a new, non-Huffman name puts the given bytes on the wire unchanged.
            foreach (var (name, value) in headers)
            {
                block.Add(0x00);
                EncodeString(block, name);
                EncodeString(block, value);
            }

            return block.ToArray();
        }

        static void EncodeIndexed(List<byte> block, int index)
        {
            EncodeInteger(block, index, prefixBits: 7, prefixValue: 0x80);
        }

        static void EncodeLiteralWithIndexedName(List<byte> block, int nameIndex, string value)
        {
            EncodeInteger(block, nameIndex, prefixBits: 4, prefixValue: 0x00);
            EncodeString(block, value);
        }

        static void EncodeString(List<byte> block, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            EncodeInteger(block, bytes.Length, prefixBits: 7, prefixValue: 0x00);
            block.AddRange(bytes);
        }

        static void EncodeInteger(List<byte> block, int value, int prefixBits, byte prefixValue)
        {
            var max = (1 << prefixBits) - 1;
            if (value < max)
            {
                block.Add((byte)(prefixValue | value));
                return;
            }

            block.Add((byte)(prefixValue | max));
            value -= max;
            while (value >= 0x80)
            {
                block.Add((byte)((value & 0x7f) | 0x80));
                value >>= 7;
            }

            block.Add((byte)value);
        }

        static async Task<RawHttpResponse> ReadHttp2ResponseAsync(Stream stream, CancellationToken cancellation)
        {
            var status = 0;
            var body = new List<byte>();

            while (true)
            {
                var header = await ReadExactlyAsync(stream, frameHeaderLength, cancellation);
                if (header == null)
                    return status == 0
                        // A peer that rejects the header block, for example an uppercase field name,
                        // may close the connection instead of sending RST_STREAM or GOAWAY.
                        ? RawHttpResponse.Refusal("closed without response")
                        : RawHttpResponse.Http(status, new List<(string, string)>(), Encoding.UTF8.GetString(body.ToArray()));

                var length = (header[0] << 16) | (header[1] << 8) | header[2];
                var type = header[3];
                var flags = header[4];
                var streamId = ((header[5] & 0x7f) << 24) | (header[6] << 16) | (header[7] << 8) | header[8];

                var payload = length == 0 ? Array.Empty<byte>() : await ReadExactlyAsync(stream, length, cancellation);
                if (payload == null)
                    return RawHttpResponse.Unavailable("connection closed inside a frame");

                switch (type)
                {
                    case frameSettings when (flags & flagAck) == 0:
                        await WriteFrameAsync(stream, frameSettings, flagAck, 0, Array.Empty<byte>(), cancellation);
                        await stream.FlushAsync(cancellation);
                        break;
                    case framePing when (flags & flagAck) == 0:
                        await WriteFrameAsync(stream, framePing, flagAck, 0, payload, cancellation);
                        await stream.FlushAsync(cancellation);
                        break;
                    case frameHeaders when streamId == 1 && status == 0:
                        status = ReadStatus(payload);
                        break;
                    case frameData when streamId == 1:
                        body.AddRange(payload);
                        break;
                    case frameRstStream when streamId == 1:
                        return RawHttpResponse.Refusal($"RST_STREAM {ErrorName(ReadErrorCode(payload, 0))}");
                    case frameGoAway:
                        return RawHttpResponse.Refusal($"GOAWAY {ErrorName(ReadErrorCode(payload, 4))}");
                }

                if (streamId == 1 && (type == frameHeaders || type == frameData) && (flags & flagEndStream) != 0)
                    return RawHttpResponse.Http(status, new List<(string, string)>(), Encoding.UTF8.GetString(body.ToArray()));
            }
        }

        // Only :status is decoded. Other fields are skipped, so no dynamic table or full Huffman
        // table is needed: :status is always sent as a static index or as a literal naming index 8.
        static int ReadStatus(byte[] block)
        {
            var index = 0;
            while (index < block.Length)
            {
                var first = block[index];
                if ((first & 0x80) != 0)
                {
                    var staticIndex = DecodeInteger(block, ref index, 7);
                    var indexed = StatusFromStaticIndex(staticIndex);
                    if (indexed != 0)
                        return indexed;

                    continue;
                }

                if ((first & 0xe0) == 0x20)
                {
                    DecodeInteger(block, ref index, 5);
                    continue;
                }

                var nameIndex = DecodeInteger(block, ref index, (first & 0xc0) == 0x40 ? 6 : 4);
                var isStatus = nameIndex == indexStatus;
                if (nameIndex == 0)
                {
                    var name = ReadStringBytes(block, ref index, out _);
                    if (name == null)
                        return 0;

                    isStatus = Encoding.ASCII.GetString(name) == ":status";
                }

                var value = ReadStringBytes(block, ref index, out var huffman);
                if (value == null)
                    return 0;

                if (!isStatus)
                    continue;

                var text = huffman ? DecodeHuffmanDigits(value) : Encoding.ASCII.GetString(value);
                return int.TryParse(text, out var status) ? status : 0;
            }

            return 0;
        }

        static int StatusFromStaticIndex(int staticIndex)
        {
            return staticIndex switch
            {
                8 => 200,
                9 => 204,
                10 => 206,
                11 => 304,
                12 => 400,
                13 => 404,
                14 => 500,
                _ => 0
            };
        }

        static int DecodeInteger(byte[] block, ref int index, int prefixBits)
        {
            var max = (1 << prefixBits) - 1;
            var value = block[index++] & max;
            if (value < max)
                return value;

            var shift = 0;
            while (index < block.Length)
            {
                var next = block[index++];
                value += (next & 0x7f) << shift;
                if ((next & 0x80) == 0)
                    break;

                shift += 7;
            }

            return value;
        }

        static byte[] ReadStringBytes(byte[] block, ref int index, out bool huffman)
        {
            huffman = false;
            if (index >= block.Length)
                return null;

            huffman = (block[index] & 0x80) != 0;
            var length = DecodeInteger(block, ref index, 7);
            if (length < 0 || index + length > block.Length)
                return null;

            var bytes = block[index..(index + length)];
            index += length;
            return bytes;
        }

        // HPACK Huffman codes for the ASCII digits (RFC 7541 appendix B). A :status value is
        // always three digits, so no other code is reachable here.
        static readonly (uint Code, int Bits, char Digit)[] huffmanDigits =
        {
            (0x00, 5, '0'), (0x01, 5, '1'), (0x02, 5, '2'),
            (0x19, 6, '3'), (0x1a, 6, '4'), (0x1b, 6, '5'), (0x1c, 6, '6'),
            (0x1d, 6, '7'), (0x1e, 6, '8'), (0x1f, 6, '9')
        };

        static string DecodeHuffmanDigits(byte[] bytes)
        {
            var digits = new StringBuilder();
            uint code = 0;
            var bits = 0;

            foreach (var current in bytes)
            {
                for (var bit = 7; bit >= 0; bit--)
                {
                    code = (code << 1) | (uint)((current >> bit) & 1);
                    bits++;

                    foreach (var (candidate, candidateBits, digit) in huffmanDigits)
                    {
                        if (candidateBits != bits || candidate != code)
                            continue;

                        digits.Append(digit);
                        code = 0;
                        bits = 0;
                        break;
                    }
                }
            }

            return digits.ToString();
        }

        static uint ReadErrorCode(byte[] payload, int offset)
        {
            if (payload.Length < offset + 4)
                return 0;

            return ((uint)payload[offset] << 24) | ((uint)payload[offset + 1] << 16)
                | ((uint)payload[offset + 2] << 8) | payload[offset + 3];
        }

        static string ErrorName(uint code)
        {
            return code switch
            {
                0 => "NO_ERROR",
                1 => "PROTOCOL_ERROR",
                2 => "INTERNAL_ERROR",
                3 => "FLOW_CONTROL_ERROR",
                4 => "SETTINGS_TIMEOUT",
                5 => "STREAM_CLOSED",
                6 => "FRAME_SIZE_ERROR",
                7 => "REFUSED_STREAM",
                8 => "CANCEL",
                9 => "COMPRESSION_ERROR",
                10 => "CONNECT_ERROR",
                11 => "ENHANCE_YOUR_CALM",
                12 => "INADEQUATE_SECURITY",
                13 => "HTTP_1_1_REQUIRED",
                _ => $"ERROR_{code}"
            };
        }

        static async Task WriteFrameAsync(
            Stream stream,
            byte type,
            byte flags,
            int streamId,
            byte[] payload,
            CancellationToken cancellation)
        {
            var header = new byte[frameHeaderLength];
            header[0] = (byte)(payload.Length >> 16);
            header[1] = (byte)(payload.Length >> 8);
            header[2] = (byte)payload.Length;
            header[3] = type;
            header[4] = flags;
            header[5] = (byte)(streamId >> 24);
            header[6] = (byte)(streamId >> 16);
            header[7] = (byte)(streamId >> 8);
            header[8] = (byte)streamId;

            await stream.WriteAsync(header, cancellation);
            if (payload.Length != 0)
                await stream.WriteAsync(payload, cancellation);
        }

        static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken cancellation)
        {
            var buffer = new byte[count];
            var read = 0;
            while (read < count)
            {
                var current = await stream.ReadAsync(buffer.AsMemory(read, count - read), cancellation);
                if (current == 0)
                    return null;

                read += current;
            }

            return buffer;
        }

        static async Task<byte[]> ReadToEndAsync(Stream stream, CancellationToken cancellation)
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(chunk, cancellation);
                if (read == 0)
                    return buffer.ToArray();

                buffer.Write(chunk, 0, read);
            }
        }

        static int IndexOfHeaderEnd(byte[] raw)
        {
            for (var index = 0; index + 3 < raw.Length; index++)
            {
                if (raw[index] == '\r' && raw[index + 1] == '\n' && raw[index + 2] == '\r' && raw[index + 3] == '\n')
                    return index;
            }

            return -1;
        }

        static string Describe(Exception ex)
        {
            return ex is OperationCanceledException
                ? $"timed out after {timeout.TotalSeconds:0} s"
                : $"{ex.GetType().Name}: {ex.Message}";
        }

        sealed class Connection : IDisposable
        {
            readonly TcpClient tcp;

            public Connection(TcpClient tcp, SslStream stream)
            {
                this.tcp = tcp;
                Stream = stream;
            }

            public SslStream Stream { get; }

            public SslApplicationProtocol Protocol => Stream.NegotiatedApplicationProtocol;

            public void Dispose()
            {
                Stream.Dispose();
                tcp.Dispose();
            }
        }
    }

    class RawHttpResponse
    {
        RawHttpResponse(int statusCode, IReadOnlyList<(string Name, string Value)> headers, string body, string failure, bool refused)
        {
            StatusCode = statusCode;
            Headers = headers;
            Body = body;
            Failure = failure;
            Refused = refused;
        }

        // HTTP status code, or 0 when the request never produced one.
        public int StatusCode { get; }

        public IReadOnlyList<(string Name, string Value)> Headers { get; }

        public string Body { get; }

        // Transport, TLS, or protocol level reason the request produced no status.
        public string Failure { get; }

        // The peer answered at the protocol level, for example RST_STREAM on a malformed request.
        public bool Refused { get; }

        public static RawHttpResponse Http(int statusCode, IReadOnlyList<(string Name, string Value)> headers, string body)
        {
            return new RawHttpResponse(statusCode, headers, body, null, refused: false);
        }

        public static RawHttpResponse Refusal(string reason)
        {
            return new RawHttpResponse(0, Array.Empty<(string, string)>(), string.Empty, reason, refused: true);
        }

        public static RawHttpResponse Unavailable(string reason)
        {
            return new RawHttpResponse(0, Array.Empty<(string, string)>(), string.Empty, reason, refused: false);
        }
    }
}
