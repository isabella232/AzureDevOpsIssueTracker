using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Octopus.Server.Extensibility.IssueTracker.AzureDevOps.AdoClients
{
    public interface IHttpJsonClient : IDisposable
    {
        (HttpStatusCode status, JObject jObject) Get(string url, string basicPassword = null);
    }

    public sealed class HttpJsonClient : IHttpJsonClient
    {
        private readonly HttpClient httpClient = new HttpClient();

        public (HttpStatusCode status, JObject jObject) Get(string url, string basicPassword = null)
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
                // Work around servers that report auth failure with redirect to a status 203 html page (in violation of our Accept header)
                if (response.StatusCode == HttpStatusCode.NonAuthoritativeInformation && response.Content?.Headers?.ContentType?.MediaType == "text/html")
                {
                    return (HttpStatusCode.Unauthorized, null);
                }

                return (
                    response.StatusCode,
                    ParseJsonOrDefault(response.Content)
                );
            }
        }

        private JObject ParseJsonOrDefault(HttpContent httpContent)
        {
            try
            {
                return JObject.Parse(httpContent.ReadAsStringAsync().GetAwaiter().GetResult());
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}