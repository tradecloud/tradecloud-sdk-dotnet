using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class MigrateSuppliersOrders
    {   
        const bool dryRun = true;
        const string adminUsername = "";
        const string adminPassword = "";
        const string tenantUsername = "";
        const string tenantPassword = "";
        const string tenantId = "";
        //const int supplierCodeLength = 10;
        static readonly string[] statuses = {"open", "inconsistent", "confirmed", "overdue", "approved", "shipped"};

        const string getCompanyUrlTemplate = "https://accp.tradecloud.nl/api/v1/company/tenant/{tenantId}/code/{code}";
        const string migrateCompanyUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{supplierId}";
        const string migrateUsersUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/company/{supplierId}/users";
        const string migrateOrderUrlTemplate = "https://accp.tradecloud.nl/api/v1/admin/migrate/order/{orderId}";
        const string searchOrdersUrlTemplate = "https://accp.tradecloud.nl/api/v1/purchaseOrder?tenantId={tenantId}&supplierId={supplierId}&status={status}&archived=false&page={page}&limit={limit}";
        const string searchPageSize = "100";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud migrate suppliers with filtered orders.");

            HttpClient httpClient = new HttpClient();
            var adminEncodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(adminUsername + ":" + adminPassword));
            var tenantEncodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(tenantUsername + ":" + tenantPassword));
                        
            using(var log = new StreamWriter("migrate-suppliers.log", append: true) )
            {
                using(var reader = new StreamReader("migrate-supplier-codes.txt"))
                {                    
                    while (!reader.EndOfStream)
                    {   
                        var supplierCode = reader.ReadLine(); //.PadLeft(supplierCodeLength, '0');
                        var getCompanyUrl = getCompanyUrlTemplate.Replace("{tenantId}", tenantId).Replace("{code}", supplierCode);
                        
                        var supplier = await GetSupplier(supplierCode, getCompanyUrl, log);
                        if (supplier != null)
                        {
                            string supplierId = supplier["id"].ToString();                            
                            var migrateCompanyUrl = migrateCompanyUrlTemplate.Replace("{supplierId}", supplierId);
                            await Migrate("supplierCode=" + supplierCode, migrateCompanyUrl, log);
                            var migrateUsersUrl = migrateUsersUrlTemplate.Replace("{supplierId}", supplierId);
                            await Migrate("supplierCode=" + supplierCode, migrateUsersUrl, log);

                            foreach (string status in statuses)
                            {
                                await MigrateOrders(supplierCode, supplierId, status, log);
                            }
                        }
                    }
                }
            }

            async Task MigrateOrders(string supplierCode, string supplierId, string status, StreamWriter log)
            {
                int page = 1;
                int totalPages = 1;
                while (page <= totalPages)                
                {
                    var searchResult = await SearchOrders(supplierCode, supplierId, status, page, log);
                    if (searchResult != null)
                    {
                       
                        page = ((int)searchResult["page"]);
                        int total = ((int)searchResult["total"]);
                        int pageSize = ((int)searchResult["pageSize"]);
                        totalPages = ((int)searchResult["totalPages"]);
                        await log.WriteLineAsync("MigrateOrders supplierCode=" + supplierCode + " supplierId=" + supplierId + " status=" + status + " page=" + page + " total=" + total + " pageSize=" + pageSize + " totalPages=" + totalPages);

                        if (total > 0)
                        {
                            foreach (var order in searchResult["data"].Children())
                            {
                                string orderId = order["id"].ToString();
                                string orderCode = order["code"].ToString();
                                var migrateOrderUrl = migrateOrderUrlTemplate.Replace("{orderId}", orderId);
                                await Migrate("orderCode=" + orderCode, migrateOrderUrl, log);
                            }
                        }
                    }
                    page++;
                }
            }

            async Task<JObject> GetSupplier(string supplierCode, string url, StreamWriter log)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", adminEncodedCredentials);
                var response = await httpClient.GetAsync(url);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("GetSupplier   supplierCode=" + supplierCode + " url=" + url + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await log.WriteLineAsync("GetSupplier   response body=" +  responseString);
                    return null;
                }                    
            }

            async Task<JObject> SearchOrders(string supplierCode, string supplierId, string status, int page, StreamWriter log)
            {
                var url = searchOrdersUrlTemplate
                    .Replace("{tenantId}", tenantId)
                    .Replace("{supplierId}", supplierId)
                    .Replace("{status}", status)
                    .Replace("{page}", page.ToString())
                    .Replace("{limit}", searchPageSize.ToString());

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", tenantEncodedCredentials);
                var response = await httpClient.GetAsync(url);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync("SearchOrders  supplierCode=" + supplierCode + " url=" + url + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await log.WriteLineAsync("SearchOrders  response body=" + responseString);
                    return null;
                }
            }

            async Task Migrate(string migration, string url, StreamWriter log)
            {
                if (dryRun) 
                {
                    await log.WriteLineAsync("Migrate       " + migration + " url=" + url + " status=dryrun");
                }
                else
                {                
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", adminEncodedCredentials);
                    var response = await httpClient.PostAsync(url, null);
                    var statusCode = (int)response.StatusCode;
                    await log.WriteLineAsync("Migrate       " + migration + " url=" + url + " status=" + statusCode + " reason=" + response.ReasonPhrase);
                    
                    if (statusCode != 200) 
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        await log.WriteLineAsync("Migrate        response body=" +  responseString);
                    }
                }                    
            }
        }
    }
}
