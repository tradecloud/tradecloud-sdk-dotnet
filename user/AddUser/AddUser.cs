using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace Com.Tradecloud1.SDK.Client
{
    class AddUser
    {   
         // https://swagger-ui.accp.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/authentication/specs.yaml#/authentication/
        const string authenticationUrl = "https://api.accp.tradecloud1.com/v2/authentication/";
        // Fill in mandatory username
        const string username = "";
        // Fill in mandatory password
        const string password = "";
        // Add user add url
        //  https://swagger-ui.s.tradecloud1.com/?url=https://api.accp.tradecloud1.com/v2/user/private/specs.yaml#/user/addUserRoute
        const string addUserUrl = "https://api.accp.tradecloud1.com/v2/user/add";

        // Check/amend new user
        const string jsonContentWithSingleQuotes = 
            @"{
                `newUserId`: `40fef20d-8769-4a0b-aa2d-90a0b00750b4`,
                `email`: `user@example.com`,
                `companyId`: `f56aa4ce-8ec8-5197-bc26-77716a58add7`,
                `companyName`: `Agrifac Machinery B.V.`,
                `roles`: [
                    `buyer`
                ],
                `profile`: {
                    `firstName`: `John`,
                    `lastName`: `Doe`,
                    `position`: `Company Employee`,
                    `phoneNumber`: `012345678`,
                    `linkedInProfile`: `linkedin/in/johndoe`,
                    `profilePictureId`: `384cb2dc-b7d9-4b55-a81b-6244fcf67c56`
                },
                `settings`: {
                    `notificationInterval`: `realtime`,
                    `externalUsername`: `johndoe`
                }
            }";
                        
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tradecloud add user example.");
            
            HttpClient httpClient = new HttpClient();
            var authenticationClient = new Authentication(httpClient, authenticationUrl);
            var (accessToken, refreshToken) = await authenticationClient.Login(username, password);
            await AddUser(accessToken);

            async Task AddUser(string accessToken)
            {                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var jsonContent = jsonContentWithSingleQuotes.Replace("`", "\"");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(addUserUrl, content);
                watch.Stop();
                Console.WriteLine("AddUser StatusCode: " + (int)response.StatusCode + " ElapsedMilliseconds: " + watch.ElapsedMilliseconds);

                string responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("AddUser Body: " +  responseString);  
            }
        }
    }
}
