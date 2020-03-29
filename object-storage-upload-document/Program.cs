using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace object_storage_upload_document
{
    class Program
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/login
        const string loginUrl = "https://api.accp.tradecloud1.com/v2/authentication/login";
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/object-storage/specs.yaml#/object-storage/uploadDocument
        const string uploadDocumentUrl = "https://api.accp.tradecloud1.com/v2/object-storage/document";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud upload document.");
            
            Console.Write("Enter username: " );
            var username = Console.ReadLine();

            Console.Write("Enter password: " );
            var password = Console.ReadLine();

            Console.Write("Enter filename: ");
            var fileName = Console.ReadLine();

            HttpClient httpClient = new HttpClient();
            var token = await Authenticate(username, password);

            async Task<string> Authenticate(string username, string password)
            {
                var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );

                var response = await httpClient.GetAsync(loginUrl);
                var token = response.Headers.GetValues("Set-Authorization").FirstOrDefault();
                var content = response.Content;

                Console.WriteLine("Authenticate StatusCode: " + (int)response.StatusCode);
                Console.WriteLine("Authenticate Set-Authorization: " + token);

                string result = await content.ReadAsStringAsync();
                Console.WriteLine("Authenticate Content: " + result);         

                return token;
            }
        }
    }
}
