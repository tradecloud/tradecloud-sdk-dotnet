using System;
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

        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        const string setAuthorization = "Set-Authorization";
        const string setRefreshToken = "Set-Refresh-Token";

        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("=== Tradecloud login HTTP header case test ===");
            Console.WriteLine();

            var user = args.Length >= 2 ? args[0] : username;
            var pass = args.Length >= 2 ? args[1] : password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine("ERROR: Fill in username and password in TestLoginHeaderCase.cs, or pass them as arguments:");
                Console.WriteLine("       dotnet run -- <username> <password>");
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
                pass);

            Console.WriteLine("=== Summary ===");
            PrintResult("HTTP/1.1 (preserveHeaderCase)", http11);
            PrintResult("HTTP/2", http2);
            PrintResult("HTTP/3", http3);

            var failed = http11 == CheckResult.Failed || http2 == CheckResult.Failed
                || http3 == CheckResult.Failed;
            return failed ? 1 : 0;
        }

        static async Task<CheckResult> LoginAndCheckHeaderCase(
            Version requestedVersion,
            HttpVersionPolicy versionPolicy,
            HeaderCase expectedHeaderCase,
            bool required,
            string user,
            string pass)
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
                Console.WriteLine();
                return CheckResult.Skipped;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL: {label} request failed ({ex.GetType().Name}: {ex.Message})");
                Console.WriteLine();
                return CheckResult.Failed;
            }

            using (response)
            {
                Console.WriteLine($"  Status: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"  Negotiated version: {response.Version}");

                Console.WriteLine("  Response header names (as received):");
                foreach (var header in response.Headers.NonValidated)
                    Console.WriteLine($"    {header.Key}");

                if (response.Version != requestedVersion)
                {
                    if (!required)
                    {
                        Console.WriteLine($"  SKIP: negotiated {response.Version} instead of {requestedVersion}");
                        Console.WriteLine();
                        return CheckResult.Skipped;
                    }

                    Console.WriteLine($"  FAIL: negotiated {response.Version}, expected {requestedVersion}");
                    Console.WriteLine();
                    return CheckResult.Failed;
                }

                if ((int)response.StatusCode != 200)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"  FAIL: login did not succeed. Body: {body}");
                    Console.WriteLine();
                    return CheckResult.Failed;
                }

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
                    return CheckResult.Passed;
                }

                Console.WriteLine($"  RESULT: FAIL - {label} token header case did not match");
                Console.WriteLine();
                return CheckResult.Failed;
            }
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

        static void PrintResult(string label, CheckResult result)
        {
            Console.WriteLine($"  {label}: {result.ToString().ToUpperInvariant()}");
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
    }
}
