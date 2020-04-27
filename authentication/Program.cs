using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class Program
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud authentication example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            
            Console.WriteLine("Login..");
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            Console.WriteLine("accessToken: " + accessToken);
            Console.WriteLine("refreshToken: " + refreshToken);

            Console.WriteLine("Refreshing...");
            var (refreshedAccessToken, refreshedRefreshToken) = await authenticationClient.Refresh(refreshToken);
            Console.WriteLine("refreshedAccessToken: " + refreshedAccessToken);
            Console.WriteLine("refreshedRefreshToken: " + refreshedRefreshToken);

            Console.WriteLine("Refreshing with invalidated refresh token...");
            var (refreshedWithInvalidAccessToken, rrefreshedWithInvalidRefreshToken) = await authenticationClient.Refresh(refreshToken);
            Console.WriteLine("refreshedWithInvalidAccessToken: " + refreshedWithInvalidAccessToken);
            Console.WriteLine("rrefreshedWithInvalidRefreshToken: " + rrefreshedWithInvalidRefreshToken);
                       
            Console.WriteLine("Logout...");
            var (loggedOutAccessToken, loggedOutRefreshToken) = await authenticationClient.Logout(refreshedRefreshToken);
            Console.WriteLine("loggedOutAccessToken: " + loggedOutAccessToken);
            Console.WriteLine("loggedOutRefreshToken: " + loggedOutRefreshToken);

            Console.WriteLine("Refreshing after logout...");
            var (refreshedAfterLogoutAccessToken, refreshedAfterLogoutRefreshToken) = await authenticationClient.Refresh(refreshedRefreshToken);
            Console.WriteLine("refreshedAfterLogoutAccessToken: " + refreshedAfterLogoutAccessToken);
            Console.WriteLine("refreshedAfterLogoutRefreshToken: " + refreshedAfterLogoutRefreshToken);
        }
    }
}
