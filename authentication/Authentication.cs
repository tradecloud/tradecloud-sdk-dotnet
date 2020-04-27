using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    public class Authentication
    { 
        HttpClient httpClient;
        string authenticationUrl;

        public Authentication(HttpClient httpClient, string authenticationUrl)
        {
            this.httpClient = httpClient;
            this.authenticationUrl = authenticationUrl;
        }

        public async Task<(string, string)> Login(string username, string password)
        {
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );

            var response = await httpClient.GetAsync(authenticationUrl + "login");
            Console.WriteLine("Login response StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Login response Content: " + responseString);         

            var accessToken = GetHeaderValue("Set-Authorization", response);
            var refreshToken = GetHeaderValue("Set-Refresh-Token", response);
            return (accessToken, refreshToken);
        }

        public async Task<(string, string)> Refresh(string refreshToken)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Refresh-Token", refreshToken);

            var response = await httpClient.GetAsync(authenticationUrl + "refresh");
            Console.WriteLine("Refresh response StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Refresh response Content: " + responseString);         

            var refreshedAccessToken = GetHeaderValue("Set-Authorization", response);
            var refreshedRefreshToken = GetHeaderValue("Set-Refresh-Token", response);
            return (refreshedAccessToken, refreshedRefreshToken);
        }

        public async Task<(string, string)> Logout(string refreshToken)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Refresh-Token", refreshToken);

            var response = await httpClient.PostAsync(authenticationUrl + "logout", null);
            Console.WriteLine("Logout response StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Logout response Content: " + responseString);         

            var loggedOutAccessToken = GetHeaderValue("Set-Authorization", response);
            var loggedOutRefreshToken = GetHeaderValue("Set-Refresh-Token", response);
            return (loggedOutAccessToken, loggedOutRefreshToken);
        }

        // Work around non-existing headers without Exception
        private string GetHeaderValue(string headerName, HttpResponseMessage message) 
        {
            IEnumerable<string> headerValues;
            string value = string.Empty;
            if (message.Headers.TryGetValues(headerName, out headerValues))
            {
                value = headerValues.FirstOrDefault();
            }     
            return value;
        }
    }
}  