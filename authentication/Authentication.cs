using System;
using System.IO;
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

        public async Task<string> Authenticate(string username, string password)
        {
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );

            var response = await httpClient.GetAsync(url);
            Console.WriteLine("Authenticate StatusCode: " + (int)response.StatusCode);
            string responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Authenticate Content: " + responseString);         

            var token = response.Headers.GetValues("Set-Authorization").FirstOrDefault();
            return token;
        }
    }
}  