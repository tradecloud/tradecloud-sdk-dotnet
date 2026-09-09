using System;
using System.IO;
using DotNetEnv;

namespace Com.Tradecloud1.SDK.Client
{
    /// <summary>
    /// Loads a project-local <c>.env</c> (copy of <c>.env.template</c>) and reads credentials.
    /// Looks next to the <c>.csproj</c> when running via <c>dotnet run</c>, else the current directory.
    /// </summary>
    public static class EnvFile
    {
        public static void Load()
        {
            var nextToProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
            if (File.Exists(nextToProject))
            {
                Env.Load(nextToProject);
                return;
            }

            var cwdEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(cwdEnv))
                Env.Load(cwdEnv);
        }

        public static string Get(string name) => Environment.GetEnvironmentVariable(name);

        public static bool TryUsernamePassword(out string username, out string password)
        {
            Load();
            username = Get("TRADECLOUD_USERNAME");
            password = Get("TRADECLOUD_PASSWORD");
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                return true;

            Console.WriteLine("ERROR: TRADECLOUD_USERNAME and TRADECLOUD_PASSWORD are not set.");
            Console.WriteLine("       Copy .env.template to .env in this project folder and fill them in.");
            username ??= "";
            password ??= "";
            return false;
        }

        public static bool TryAccessToken(out string accessToken)
        {
            Load();
            accessToken = Get("ACCESS_TOKEN");
            if (!string.IsNullOrWhiteSpace(accessToken))
                return true;

            Console.WriteLine("ERROR: ACCESS_TOKEN is not set.");
            Console.WriteLine("       Copy .env.template to .env in this project folder and fill it in.");
            accessToken ??= "";
            return false;
        }
    }
}
