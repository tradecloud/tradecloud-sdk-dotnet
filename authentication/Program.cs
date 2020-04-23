using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class Program
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/login";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud authentication example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            
            Console.WriteLine("Authenticating..");
            var (accessToken, refreshToken) = await authenticationClient.Authenticate(username, password);
            Console.WriteLine("accessToken: " + accessToken);
            Console.WriteLine("refreshToken: " + refreshToken);

            Console.WriteLine("Refreshing...");
            var (refreshedAccessToken, refreshedRefreshToken) = await authenticationClient.Refresh(refreshToken);
            Console.WriteLine("refreshedAccessToken: " + refreshedAccessToken);
            Console.WriteLine("refreshedRefreshToken: " + refreshedRefreshToken);

            Console.WriteLine("Logout...");
            var (loggedOutAccessToken, loggedOutRefreshToken) = await authenticationClient.Logout(refreshedAccessToken);
            Console.WriteLine("loggedOutAccessToken: " + loggedOutAccessToken);
            Console.WriteLine("loggedOutRefreshToken: " + loggedOutRefreshToken);

            Console.WriteLine("Refreshing after logout...");
            var (refreshedAfterLogoutAccessToken, refreshedAfterLogoutRefreshToken) = await authenticationClient.Refresh(refreshToken);
            Console.WriteLine("refreshedAfterLogoutAccessToken: " + refreshedAfterLogoutAccessToken);
            Console.WriteLine("refreshedAfterLogoutRefreshToken: " + refreshedAfterLogoutRefreshToken);
        }
    }
}
