using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class TestLoginHeaderCase
    {
        const string baseUrl = "https://api.accp.tradecloud1.com";
        static readonly string loginUrl = $"{baseUrl}/v2/authentication/login";
        static readonly string orderUrlPrefix = $"{baseUrl}/v2/order/";

        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";
        // Fill in order id used to probe Authorization header casing (GET /v2/order/{orderId})
        const string orderId = "";

        const string setAuthorization = "Set-Authorization";
        const string setRefreshToken = "Set-Refresh-Token";

        // Lowercase is the only field name casing HTTP/2 allows, so the scheme variants are tested
        // there. The other name casings only need one scheme to show whether the name survives.
        static readonly (string headerName, string scheme)[] authorizationCases =
        {
            ("authorization", "Bearer"),
            ("authorization", "bearer"),
            ("authorization", "BEARER"),
            ("authorization", "BeArEr"),
            ("Authorization", "bearer"),
            ("AUTHORIZATION", "bearer")
        };

        static readonly Version[] authorizationHttpVersions =
        {
            HttpVersion.Version11,
            HttpVersion.Version20
        };

        // Accepted first, then the requests the server refused outright, then the rest.
        static readonly ProbeOutcome[] summaryOutcomeOrder =
        {
            ProbeOutcome.Accepted,
            ProbeOutcome.Malformed,
            ProbeOutcome.Rejected,
            ProbeOutcome.Unexpected,
            ProbeOutcome.Error
        };

        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("=== Tradecloud login HTTP header case test ===");
            Console.WriteLine();

            var user = args.Length >= 2 ? args[0] : username;
            var pass = args.Length >= 2 ? args[1] : password;
            var configuredOrderId = args.Length >= 3 ? args[2] : orderId;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine("ERROR: Fill in username and password in TestLoginHeaderCase.cs, or pass them as arguments:");
                Console.WriteLine("       dotnet run -- <username> <password> [orderId]");
                return 1;
            }

            // Required for HTTP/3 on .NET 8 (also needs libmsquic on Linux).
            AppContext.SetSwitch("System.Net.SocketsHttpHandler.Http3Support", true);

            var http11 = await LoginAndCheckHeaderCase(
                HttpVersion.Version11,
                HttpVersionPolicy.RequestVersionExact,
                expectedHeaderCase: HeaderCase.Preserved,
                required: true,
                user,
                pass);

            var http2 = await LoginAndCheckHeaderCase(
                HttpVersion.Version20,
                HttpVersionPolicy.RequestVersionExact,
                expectedHeaderCase: HeaderCase.Lower,
                required: true,
                user,
                pass);

            var http3 = await LoginAndCheckHeaderCase(
                HttpVersion.Version30,
                HttpVersionPolicy.RequestVersionExact,
                expectedHeaderCase: HeaderCase.Lower,
                required: false,
                user,
                pass,
                altSvc: http2.AltSvc ?? http11.AltSvc);

            var login = LatestLoginWithToken(("HTTP/1.1", http11), ("HTTP/2", http2), ("HTTP/3", http3));
            var (authorizationSkip, authorizationResults) = await GetOrderWithAuthorizationCases(
                configuredOrderId,
                login.AccessToken,
                login.Source);

            PrintSummary(http11.Result, http2.Result, http3.Result, authorizationSkip, authorizationResults);

            var failed = http11.Result == CheckResult.Failed || http2.Result == CheckResult.Failed
                || http3.Result == CheckResult.Failed
                || authorizationResults.Exists(item => item.Outcome == ProbeOutcome.Error)
                || authorizationResults.Exists(item => IsCanonical(item) && item.Outcome != ProbeOutcome.Accepted);
            return failed ? 1 : 0;
        }

        // The casing a compliant client would send must be accepted; the variants only get
        // reported. A lowercase name is valid on every version, HTTP/2 included.
        static bool IsCanonical(ProbeResult probe)
        {
            return probe.HeaderName == "authorization" && probe.Scheme == "Bearer";
        }

        static async Task<LoginResult> LoginAndCheckHeaderCase(
            Version requestedVersion,
            HttpVersionPolicy versionPolicy,
            HeaderCase expectedHeaderCase,
            bool required,
            string user,
            string pass,
            string altSvc = null)
        {
            var label = $"HTTP/{requestedVersion}";
            Console.WriteLine($"--- {label} ---");
            Console.WriteLine($"  GET {loginUrl}");
            Console.WriteLine($"  Requested version: {requestedVersion} ({versionPolicy})");
            Console.WriteLine($"  Expected token header names: {ExpectedNames(expectedHeaderCase)}");

            // Talk to the API directly so a local HTTP proxy cannot pin the connection to HTTP/1.1.
            using var handler = new SocketsHttpHandler { UseProxy = false };
            using var httpClient = new HttpClient(handler)
            {
                DefaultRequestVersion = requestedVersion,
                DefaultVersionPolicy = versionPolicy
            };

            using var request = new HttpRequestMessage(HttpMethod.Get, loginUrl)
            {
                Version = requestedVersion,
                VersionPolicy = versionPolicy
            };
            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + pass));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception ex) when (!required)
            {
                Console.WriteLine($"  SKIP: {label} is not available ({ex.GetType().Name}: {ex.Message})");
                Console.WriteLine($"  WHY:  {ExplainMissingHttp3(altSvc)}");
                Console.WriteLine();
                return LoginResult.Skipped();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL: {label} request failed ({ex.GetType().Name}: {ex.Message})");
                Console.WriteLine();
                return LoginResult.Failed(null);
            }

            using (response)
            {
                Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"  Negotiated version: {response.Version}");

                Console.WriteLine("  Response header names (as received):");
                foreach (var header in response.Headers.NonValidated)
                    Console.WriteLine($"    {header.Key}");

                var advertised = FindHeaderValue(response, "Alt-Svc");

                if (response.Version != requestedVersion)
                {
                    if (!required)
                    {
                        Console.WriteLine($"  SKIP: negotiated {response.Version} instead of {requestedVersion}");
                        Console.WriteLine($"  WHY:  {ExplainMissingHttp3(altSvc ?? advertised)}");
                        Console.WriteLine();
                        return LoginResult.Skipped();
                    }

                    Console.WriteLine($"  FAIL: negotiated {response.Version}, expected {requestedVersion}");
                    Console.WriteLine();
                    return LoginResult.Failed(null);
                }

                if ((int)response.StatusCode != 200)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"  FAIL: login did not succeed. Body: {body}");
                    Console.WriteLine();
                    return LoginResult.Failed(null);
                }

                var accessToken = FindHeaderValue(response, setAuthorization);
                var authorizationName = FindHeaderName(response, setAuthorization);
                var refreshTokenName = FindHeaderName(response, setRefreshToken);
                var expectedAuthorization = ExpectedName(setAuthorization, expectedHeaderCase);
                var expectedRefresh = ExpectedName(setRefreshToken, expectedHeaderCase);

                var authorizationOk = CheckTokenHeader(setAuthorization, authorizationName, expectedAuthorization);
                var refreshOk = CheckTokenHeader(setRefreshToken, refreshTokenName, expectedRefresh);

                Console.WriteLine();
                if (authorizationOk && refreshOk)
                {
                    Console.WriteLine($"  RESULT: PASS - {label} token headers use {CaseLabel(expectedHeaderCase)}");
                    Console.WriteLine();
                    return LoginResult.Passed(accessToken, advertised);
                }

                Console.WriteLine($"  RESULT: FAIL - {label} token header case did not match");
                Console.WriteLine();
                return LoginResult.Failed(accessToken);
            }
        }

        // HTTP/3 needs a QUIC listener on the server and QUIC support in the client. A server that
        // offers HTTP/3 advertises it in Alt-Svc, so its absence points at the server side.
        static string ExplainMissingHttp3(string altSvc)
        {
            if (string.IsNullOrEmpty(altSvc))
                return "server side: no Alt-Svc header on the HTTP/1.1 or HTTP/2 response, so the API does not offer HTTP/3";

            if (!altSvc.Contains("h3", StringComparison.OrdinalIgnoreCase))
                return $"server side: Alt-Svc '{altSvc}' does not offer h3";

            return $"client side: the server advertises '{altSvc}', so this host lacks QUIC support "
                + "(on Linux .NET needs libmsquic)";
        }

        static async Task<(string SkipReason, List<ProbeResult> Results)> GetOrderWithAuthorizationCases(
            string configuredOrderId,
            string accessToken,
            string tokenSource)
        {
            var results = new List<ProbeResult>();

            Console.WriteLine("=== Authorization header case (GET /v2/order/{orderId}) ===");
            Console.WriteLine();

            if (string.IsNullOrEmpty(configuredOrderId))
            {
                Console.WriteLine("SKIP: Fill in orderId in TestLoginHeaderCase.cs, or pass it as the third argument:");
                Console.WriteLine("      dotnet run -- <username> <password> <orderId>");
                Console.WriteLine();
                return ("no orderId", results);
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("SKIP: no access token from login");
                Console.WriteLine();
                return ("no access token from login", results);
            }

            var token = StripScheme(accessToken);

            Console.WriteLine($"  Order id: {configuredOrderId}");
            Console.WriteLine($"  GET {orderUrlPrefix}{configuredOrderId}");
            Console.WriteLine($"  Access token: from the {tokenSource} login above ({token.Length} chars)");
            Console.WriteLine("  Applies that access token with Authorization name and Bearer scheme casing variants.");
            Console.WriteLine("  RawHttpClient writes the header bytes itself, so each name reaches the wire as given.");
            Console.WriteLine("  Uppercase field names are malformed for HTTP/2 (RFC 9113), so a reject there is expected.");
            Console.WriteLine();

            foreach (var version in authorizationHttpVersions)
            {
                foreach (var (headerName, scheme) in authorizationCases)
                {
                    var probe = await GetOrderWithAuthorizationCase(
                        configuredOrderId,
                        token,
                        headerName,
                        scheme,
                        version);
                    results.Add(probe);
                }
            }

            return (null, results);
        }

        static async Task<ProbeResult> GetOrderWithAuthorizationCase(
            string configuredOrderId,
            string accessToken,
            string headerName,
            string scheme,
            Version requestedVersion)
        {
            var uri = new Uri(orderUrlPrefix + configuredOrderId);
            var label = $"HTTP/{requestedVersion} {headerName}: {scheme} <access-token>";
            Console.WriteLine($"--- {label} ---");
            Console.WriteLine($"  GET {uri}");
            Console.WriteLine($"  Wire header: '{headerName}: {scheme} <access-token>'");

            var headers = new[] { (headerName, scheme + " " + accessToken) };
            var response = requestedVersion == HttpVersion.Version20
                ? await RawHttpClient.GetHttp2Async(uri, headers)
                : await RawHttpClient.GetHttp11Async(uri, headers);

            var probe = Classify(headerName, scheme, requestedVersion, response);
            if (response.StatusCode != 0)
                Console.WriteLine($"  Status: {response.StatusCode}");
            else
                Console.WriteLine($"  No status: {response.Failure}");

            if (response.StatusCode != 0 && response.StatusCode != 200 && !string.IsNullOrEmpty(response.Body))
                Console.WriteLine($"  Body: {Shorten(response.Body, 200)}");

            Console.WriteLine($"  RESULT: {probe.Outcome.ToString().ToUpperInvariant()} - {probe.Detail}");
            Console.WriteLine();
            return probe;
        }

        static ProbeResult Classify(string headerName, string scheme, Version version, RawHttpResponse response)
        {
            if (response.Refused)
                return new ProbeResult(headerName, scheme, version, ProbeOutcome.Malformed, response.Failure);

            if (response.StatusCode == 0)
                return new ProbeResult(headerName, scheme, version, ProbeOutcome.Error, response.Failure);

            var outcome = response.StatusCode switch
            {
                200 => ProbeOutcome.Accepted,
                400 => ProbeOutcome.Malformed,
                401 => ProbeOutcome.Rejected,
                403 => ProbeOutcome.Rejected,
                _ => ProbeOutcome.Unexpected
            };
            return new ProbeResult(headerName, scheme, version, outcome, response.StatusCode.ToString());
        }

        static string Shorten(string text, int length)
        {
            var single = text.Replace("\r", string.Empty).Replace("\n", " ").Trim();
            return single.Length <= length ? single : single.Substring(0, length) + "...";
        }

        // Takes the token from the last login that produced one: an earlier login may already be
        // superseded, and a stale token would show up as a rejected casing.
        static (string AccessToken, string Source) LatestLoginWithToken(
            params (string Version, LoginResult Login)[] logins)
        {
            var accessToken = (string)null;
            var source = (string)null;

            foreach (var (version, login) in logins)
            {
                if (string.IsNullOrEmpty(login.AccessToken))
                    continue;

                accessToken = login.AccessToken;
                source = version;
            }

            return (accessToken, source);
        }

        // The Set-Authorization header carries a bare token. Strip a scheme if the API ever starts
        // sending one, so the probe controls the scheme casing instead of sending it twice.
        static string StripScheme(string accessToken)
        {
            var separator = accessToken.IndexOf(' ');
            if (separator <= 0)
                return accessToken;

            var prefix = accessToken.Substring(0, separator);
            return string.Equals(prefix, "Bearer", StringComparison.OrdinalIgnoreCase)
                ? accessToken.Substring(separator + 1).Trim()
                : accessToken;
        }

        static bool CheckTokenHeader(string logicalName, string actualName, string expectedName)
        {
            if (actualName == null)
            {
                Console.WriteLine($"  {logicalName}: MISSING");
                return false;
            }

            var match = actualName == expectedName;
            Console.WriteLine($"  {logicalName}: '{actualName}' (expected '{expectedName}') {(match ? "OK" : "MISMATCH")}");
            return match;
        }

        static string FindHeaderName(HttpResponseMessage response, string headerName)
        {
            foreach (var header in response.Headers.NonValidated)
            {
                if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
                    return header.Key;
            }

            return null;
        }

        static string FindHeaderValue(HttpResponseMessage response, string headerName)
        {
            foreach (var header in response.Headers.NonValidated)
            {
                if (!string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var value in header.Value)
                    return value;
            }

            return null;
        }

        static string ExpectedName(string canonicalName, HeaderCase headerCase)
        {
            return headerCase == HeaderCase.Lower
                ? canonicalName.ToLowerInvariant()
                : canonicalName;
        }

        static string ExpectedNames(HeaderCase headerCase)
        {
            return $"{ExpectedName(setAuthorization, headerCase)}, {ExpectedName(setRefreshToken, headerCase)}";
        }

        static string CaseLabel(HeaderCase headerCase)
        {
            return headerCase == HeaderCase.Lower
                ? "lowercase names (HTTP/2 and HTTP/3 wire format)"
                : "preserved capital letters (HTTP/1.1 preserveHeaderCase)";
        }

        static void PrintSummary(
            CheckResult http11,
            CheckResult http2,
            CheckResult http3,
            string authorizationSkip,
            List<ProbeResult> authorizationResults)
        {
            Console.WriteLine("=== Summary ===");
            Console.WriteLine();
            Console.WriteLine("Login: token response header names");
            PrintLoginSummaryLine("HTTP/1.1", HeaderCase.Preserved, http11);
            PrintLoginSummaryLine("HTTP/2", HeaderCase.Lower, http2);
            PrintLoginSummaryLine("HTTP/3", HeaderCase.Lower, http3);

            Console.WriteLine();
            Console.WriteLine("GET /v2/order/{orderId}: Authorization + Bearer casing, sent as raw wire bytes");
            if (authorizationSkip != null)
            {
                Console.WriteLine($"  SKIPPED  {authorizationSkip}");
                Console.WriteLine();
                return;
            }

            Console.WriteLine();
            foreach (var outcome in summaryOutcomeOrder)
            {
                var group = authorizationResults.FindAll(probe => probe.Outcome == outcome);
                if (group.Count == 0)
                    continue;

                Console.WriteLine($"  {outcome.ToString().ToUpperInvariant()} -- {OutcomeMeaning(outcome)}");
                foreach (var probe in group)
                {
                    var version = $"HTTP/{probe.Version}";
                    var wireHeader = $"{probe.HeaderName}: {probe.Scheme}";
                    Console.WriteLine($"    {version.PadRight(9)}{wireHeader.PadRight(28)}{probe.Detail}");
                }

                Console.WriteLine();
            }
        }

        static string OutcomeMeaning(ProbeOutcome outcome)
        {
            return outcome switch
            {
                ProbeOutcome.Accepted => "200, the casing was honoured",
                ProbeOutcome.Malformed => "400 or a refused HTTP/2 header block, the request never reached authorization",
                ProbeOutcome.Rejected => "401 or 403, the casing was not honoured",
                ProbeOutcome.Unexpected => "another status, so the order id or account may be wrong",
                _ => "transport, TLS, or ALPN failure, so nothing was measured"
            };
        }

        static void PrintLoginSummaryLine(string version, HeaderCase headerCase, CheckResult result)
        {
            Console.WriteLine($"  {version.PadRight(8)}  {ExpectedNames(headerCase),-42}  {result.ToString().ToUpperInvariant()}");
        }

        enum HeaderCase
        {
            Preserved,
            Lower
        }

        enum CheckResult
        {
            Passed,
            Failed,
            Skipped
        }

        enum ProbeOutcome
        {
            Accepted,
            Rejected,
            Malformed,
            Unexpected,
            Error
        }

        class LoginResult
        {
            LoginResult(CheckResult result, string accessToken, string altSvc)
            {
                Result = result;
                AccessToken = accessToken;
                AltSvc = altSvc;
            }

            public CheckResult Result { get; }

            public string AccessToken { get; }

            // Alt-Svc as received, used to tell a missing server HTTP/3 listener from a client
            // without QUIC support.
            public string AltSvc { get; }

            public static LoginResult Passed(string accessToken, string altSvc)
            {
                return new LoginResult(CheckResult.Passed, accessToken, altSvc);
            }

            public static LoginResult Failed(string accessToken)
            {
                return new LoginResult(CheckResult.Failed, accessToken, null);
            }

            public static LoginResult Skipped()
            {
                return new LoginResult(CheckResult.Skipped, null, null);
            }
        }

        class ProbeResult
        {
            public ProbeResult(string headerName, string scheme, Version version, ProbeOutcome outcome, string detail)
            {
                HeaderName = headerName;
                Scheme = scheme;
                Version = version;
                Outcome = outcome;
                Detail = detail;
            }

            public string HeaderName { get; }

            public string Scheme { get; }

            public Version Version { get; }

            public ProbeOutcome Outcome { get; }

            public string Detail { get; }
        }
    }
}
