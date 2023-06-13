using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class MigrateSuppliers
    {   
        // Fill in the mandatory username
       const string username = "";
        // Fill in mandatory password
        const string password = "";


        const string tenantId = "";

        const string getCompanyUrlTemplate = "https://portal.tradecloud.nl/api/v1/company/tenant/{tenantId}/code/{code}";
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
                using(var reader = new StreamReader("migrate-supplier-codes.txt"))
                {                    
                    while (!reader.EndOfStream)
                    {   
                        var supplierCode = reader.ReadLine();
                        var getCompanyUrl = getCompanyUrlTemplate.Replace("{tenantId}", tenantId).Replace("{code}", supplierCode);
                        
                        var queryResult = await GetSupplier(getCompanyUrl, log);
                        if (queryResult != null)
                        {
                           string companyId = queryResult["id"].ToString();
                           var migrateCompanyUrl = migrateCompanyUrlTemplate.Replace("{companyId}", companyId);
                           await MigrateSupplier(migrateCompanyUrl, log);
                           Thread.Sleep(2000);
                           var migrateUsersUrl = migrateUsersUrlTemplate.Replace("{companyId}", companyId);
                           await MigrateSupplier(migrateUsersUrl, log);
                           Thread.Sleep(2000);
                           var migrateOrdersUrl = migrateOrdersUrlTemplate.Replace("{companyId}", companyId);
                           await MigrateSupplier(migrateOrdersUrl, log);
                           Thread.Sleep(5000);
                        }
                    }
                }
            }

            async Task<JObject> GetSupplier(string url, StreamWriter log)
            {                
                var response = await httpClient.GetAsync(url);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("MigrateSupplier url=" + url + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await log.WriteLineAsync("MigrateSupplier response body=" +  responseString);
                    return null;
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
