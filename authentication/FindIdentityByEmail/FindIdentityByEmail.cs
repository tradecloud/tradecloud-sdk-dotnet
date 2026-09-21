using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Tradecloud1.SDK.Client
{
    class FindIdentityByEmail
    {   
        // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/internal/specs.yaml#/authentication/findIdentityByEmail
        const string findIdentityByEmailUrl = "https://api.accp.tradecloud1.com/v2/authentication/find";

        const string jsonContentWithSingleQuotes = 
            @"{
               `email`: `<email>`
            }";

        static async Task Main(string[] args)
        {
            EnvFile.Load();
            if (!EnvFile.TryAccessToken(out var accessToken))
                return;

            Console.WriteLine("Tradecloud find identity by email example.");
            
            HttpClient httpClient = new HttpClient();
            await FindIdentityByEmailRequest(accessToken);

            async Task FindIdentityByEmailRequest(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var start = DateTime.Now;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(findIdentityByEmailUrl, content);
                watch.Stop();

                var statusCode = (int)response.StatusCode;
                Console.WriteLine("FindIdentityByEmail start=" + start +  " elapsed=" + watch.ElapsedMilliseconds + "ms status=" + statusCode + " reason=" + response.ReasonPhrase);

                string responseString = await response.Content.ReadAsStringAsync();
                if (statusCode == 200)
                    Console.WriteLine("FindIdentityByEmail response body=" +  JValue.Parse(responseString).ToString(Formatting.Indented));
                else
                    Console.WriteLine("FindIdentityByEmail response body=" +  responseString);
            }
        }
    }
}
