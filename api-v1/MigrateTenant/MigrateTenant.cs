using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class MigrateTenant
    {   
        // Fill in the mandatory username
       const string username = "";
        // Fill in mandatory password
        const string password = "";
        // Tenant id to migrate
        const string tenantId = "";

        const string migrateCompanyUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{companyId}";
        const string migrateUsersUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{companyId}/users";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud migrate tenant.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
                        
            using(var log = new StreamWriter("migrate-tenant.log", append: true) )
            {
                var migrateCompanyUrl = migrateCompanyUrlTemplate.Replace("{companyId}", tenantId);
                await Migrate(migrateCompanyUrl, log);
                Thread.Sleep(2000);
                var migrateUsersUrl = migrateUsersUrlTemplate.Replace("{companyId}", tenantId);
                await Migrate(migrateUsersUrl, log);
            }

            async Task Migrate(string url, StreamWriter log)
            {                
                var response = await httpClient.PostAsync(url, null);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("Migrate url=" + url + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                
                if (statusCode != 200) 
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    await log.WriteLineAsync("Migrate response body=" +  responseString);
                }                    
            }
        }
    }
}
