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
        const string username = "frankjan@tradecloud1.com";
        // Fill in mandatory password
        const string password = "SecretSecret1";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud authentication example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var token = await authenticationClient.Authenticate(username, password);
            Console.WriteLine("Token: " + token);
        }
    }
}
