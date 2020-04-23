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
        string url;

        public Authentication(HttpClient httpClient, string url)
        {
            this.httpClient = httpClient;
            this.url = url;
        }

        public async Task<(string, string)> Authenticate(string username, string password)
        {
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );

            var response = await httpClient.GetAsync(url);
            Console.WriteLine("Authenticate StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Authenticate Content: " + responseString);         

            var accessToken = GetHeaderValue("Set-Authorization", response);
            var refreshToken = GetHeaderValue("Set-Refresh-Token", response);
            return (accessToken, refreshToken);
        }

        public async Task<(string, string)> Refresh(string refreshToken)
        {
            httpClient.DefaultRequestHeaders.Add("Refresh-Token", refreshToken);

            var response = await httpClient.GetAsync(url);
            Console.WriteLine("Refresh StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Refresh Content: " + responseString);         

            var refreshedAccessToken = GetHeaderValue("Set-Authorization", response);
            var refreshedRefreshToken = GetHeaderValue("Set-Refresh-Token", response);
            return (refreshedAccessToken, refreshedRefreshToken);
        }

        public async Task<(string, string)> Logout(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync(url);
            Console.WriteLine("Logout StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Logout Content: " + responseString);         

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