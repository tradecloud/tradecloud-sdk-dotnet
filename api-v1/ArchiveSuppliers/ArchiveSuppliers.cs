using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class ArchiveSuppliers
    {   
        // Fill in the mandatory username
        // USE A TENANT USER, NOT A SUPER USER
        const string username = "";
        // Fill in the mandatory password
        const string password = "";

        // API endpoints
        const string getCompaniesUrl = "https://accp.tradecloud.nl/api/v1/company?limit=100";
        const string archiveCompanyUrlTemplate = "https://accp.tradecloud.nl/api/v1/company/{companyId}";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud archive suppliers example.");

            HttpClient httpClient = new HttpClient();
            var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);

            using (var log = new StreamWriter("archive_suppliers.log", append: true))
            {
                while (true)
                {
                    var companiesResponse = await GetCompanies(log);
                    if (companiesResponse == null || !companiesResponse["data"].HasValues)
                    {
                        await log.WriteLineAsync("No more companies found");
                        break;
                    }

                    var companies = (JArray)companiesResponse["data"];
                    await log.WriteLineAsync($"Found {companies.Count} companies to process");
                    
                    foreach (var company in companies)
                    {
                        string companyId = company["id"].ToString();
                        await ArchiveCompany(companyId, log);
                    }
                    // we do not need pagination because archived companies are not returned in the response
                }
            }

            async Task<JObject> GetCompanies(StreamWriter log)
            {
                var response = await httpClient.GetAsync(getCompaniesUrl);
                var statusCode = (int)response.StatusCode;
                await log.WriteLineAsync($"GetCompanies status={statusCode} reason={response.ReasonPhrase}");
                
                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    await log.WriteLineAsync($"GetCompanies response body={responseString}");
                    return null;
                }
            }

            async Task ArchiveCompany(string companyId, StreamWriter log)
            {
                var url = archiveCompanyUrlTemplate.Replace("{companyId}", companyId);
                var response = await httpClient.DeleteAsync(url);
                var statusCode = (int)response.StatusCode;
                
                await log.WriteLineAsync($"ArchiveCompany companyId={companyId} status={statusCode} reason={response.ReasonPhrase}");
                
                if (statusCode != 200)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    await log.WriteLineAsync($"ArchiveCompany response body={responseString}");
                }
            }
        }
    }
}
