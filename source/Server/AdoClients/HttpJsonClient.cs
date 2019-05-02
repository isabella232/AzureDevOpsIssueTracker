using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public interface IHttpJsonClient : IDisposable
    {
        HttpRequestHeaders DefaultRequestHeaders { get; }
        JObject Get(string url);
    }

    public sealed class HttpJsonClient : IHttpJsonClient
    {
        private readonly HttpClient httpClient = new HttpClient();

        public HttpRequestHeaders DefaultRequestHeaders => httpClient.DefaultRequestHeaders;

        public JObject Get(string url)
        {
            using (var response = httpClient.GetAsync(url).GetAwaiter().GetResult())
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