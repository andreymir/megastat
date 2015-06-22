using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Newtonsoft.Json;

namespace MegafonStats
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Configuration();
            config
                .AddJsonFile("config.json")
                .AddCommandLine(args);

            var baseUrl = new Uri(config.Get("baseUrl"), UriKind.Absolute);
            var username = config.Get("username");
            var password = config.Get("password");
            var email = config.Get("email");
            var format = config.Get("format");
            
            var fromDate = DateTime.UtcNow.Date.AddDays(-1);
            var toDate = fromDate.AddDays(1);

            SubmitStatsRequest(baseUrl, username, password, fromDate, toDate, format, email).Wait();
        }

        private static async Task<bool> SubmitStatsRequest(Uri baseUrl, string username, string password,
            DateTime fromDate, DateTime toDate, string exportFormat, string email)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Requesting detalization report for {0}.", username);
            sb.AppendLine();
            sb.AppendFormat("Date: {0} - {1}", 
                fromDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                toDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
            sb.AppendLine();
            sb.AppendLine("Format: " + exportFormat);
            sb.AppendLine("Email: " + email);
            sb.AppendLine();
            
            Console.WriteLine(sb);
            
            var cookies = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cookies })
            using (var http = new HttpClient(handler))
            {
                http.DefaultRequestHeaders.Add("User-Agent", "MLK Android Phone 1.1.9");

                Console.Write("Check authentication: ");
                var url = new Uri(baseUrl, "auth/check");
                url = new UriBuilder("http", url.Host, 80, url.AbsolutePath).Uri;
                var authCheckResult = await http.GetAsync(url);
                Console.WriteLine(await authCheckResult.Content.ReadAsStringAsync());

                Console.Write("Login: ");
                var loginContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("login", username),
                    new KeyValuePair<string, string>("password", password)
                });
                var loginResult = await http.PostAsync(new Uri(baseUrl, "login"), loginContent);
                Console.WriteLine(await loginResult.Content.ReadAsStringAsync());

                Console.Write("Request detalization: ");
                url = new Uri(baseUrl, "api/reports/detalization?checkMode=false");
                var reportContent = new StringContent(JsonConvert.SerializeObject(new
                {
                    dateFrom = fromDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    email = email,
                    dateTo = toDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    format = exportFormat
                }), Encoding.UTF8, "application/json");
                var detalizationReportResult = await http.PostAsync(url, reportContent);
                Console.WriteLine(await detalizationReportResult.Content.ReadAsStringAsync());

                return true;
            }
        }
    }
}
