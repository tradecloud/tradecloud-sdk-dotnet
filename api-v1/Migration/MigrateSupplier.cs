using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class MigrateSupplier
    {   
        // Fill in the mandatory username
       const string username = "";
        // Fill in mandatory password
        const string password = "";

        const string migrateCompanyUrlTemplate = "https://portal.tradecloud.nl/api/v1/admin/migrate/company/{companyId}";
        const string migrateUsersUrlTemplate = "https://portal.tradecloud.nl/api/v1/admin/migrate/company/{companyId}/users";
        const string migrateOrdersUrlTemplate = "https://portal.tradecloud.nl/api/v1/admin/migrate/company/{companyId}/orders";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud migrate suppliers.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
                        
            using(var log = new StreamWriter("migrate-suppliers.log", append: true) )
            {
                using(var reader = new StreamReader("migrate-supplier-companyids.txt"))
                {                    
                    while (!reader.EndOfStream)
                    {   
                        var companyId = reader.ReadLine();
                        
                        var migrateCompanyUrl = migrateCompanyUrlTemplate.Replace("{companyId}", companyId);
                        await MigrateSupplier(migrateCompanyUrl, log);
                        var migrateUsersUrl = migrateUsersUrlTemplate.Replace("{companyId}", companyId);
                        await MigrateSupplier(migrateUsersUrl, log);
                        var migrateOrdersUrl = migrateOrdersUrlTemplate.Replace("{companyId}", companyId);
                        await MigrateSupplier(migrateOrdersUrl, log);
                    }
                }
            }

            async Task MigrateSupplier(string url, StreamWriter log)
            {                
                var response = await httpClient.PostAsync(url, null);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("MigrateSupplier url=" + url + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                
                if (statusCode != 200) 
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    await log.WriteLineAsync("MigrateSupplier response body=" +  responseString);
                }                    
            }
        }
    }
}
