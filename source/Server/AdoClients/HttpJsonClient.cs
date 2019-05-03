using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public interface IHttpJsonClient : IDisposable
    {
        JObject Get(string url, string basicPassword = null);
    }

    public sealed class HttpJsonClient : IHttpJsonClient
    {
        private readonly HttpClient httpClient = new HttpClient();

        public JObject Get(string url, string basicPassword = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(basicPassword))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(":" + basicPassword)));
            }

            using (var response = httpClient.SendAsync(request).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                return JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}