using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class MigrateSupplierIds
    {   
        // Fill in the mandatory username
       const string username = "";
        // Fill in mandatory password
        const string password = "";

        const string migrateCompanyUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{companyId}";
        const string migrateUsersUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{companyId}/users";
        const string migrateOrdersUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{companyId}/orders";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud migrate suppliers based on company id.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword );
                        
            using(var log = new StreamWriter("migrate-supplier-ids.log", append: true) )
            {
                using(var reader = new StreamReader("migrate-supplier-ids.txt"))
                {                    
                    while (!reader.EndOfStream)
                    {   
                        var supplierId = reader.ReadLine();
                        
                        var migrateCompanyUrl = migrateCompanyUrlTemplate.Replace("{companyId}", supplierId);
                        await MigrateSupplier(migrateCompanyUrl, log);
                        Thread.Sleep(2000);
                        var migrateUsersUrl = migrateUsersUrlTemplate.Replace("{companyId}", supplierId);
                        await MigrateSupplier(migrateUsersUrl, log);
                        Thread.Sleep(2000);
                        var migrateOrdersUrl = migrateOrdersUrlTemplate.Replace("{companyId}", supplierId);
                        await MigrateSupplier(migrateOrdersUrl, log);
                        Thread.Sleep(5000);
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
