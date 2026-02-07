using Data.Repository.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public class HttpClientServiceBase
    {
        protected HttpClient httpClient;
        protected readonly ILogger<HttpClientServiceBase> logger;

        public string AuthToken { get; set; }
        public string BaseUrl { get; set; }

        public HttpClientServiceBase(HttpClient httpClient, ILogger<HttpClientServiceBase> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task Authenticate(string url, string body,
            Func<HttpResponseMessage, Task<string>> parseToken,
            CancellationToken cancellationToken)
        {
            var authRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Content = new StringContent(body,
                    Encoding.UTF8, "application/json")
            };

            using (var response = await httpClient.SendAsync(authRequest, cancellationToken))
            {
                AuthToken = await parseToken(response);
            }
        }

        public async Task StreamAsStringAsync(string url, object request,
            Func<string, Task> received,
            CancellationToken cancellationToken = default(CancellationToken),
            Action<HttpRequestMessage> useRequest = null)
        {
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{BaseUrl}{url}"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(request),
                    Encoding.UTF8, "application/json")
            };
            EnsureAuthentication(httpRequest);
            useRequest?.Invoke(httpRequest);

            using (var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // В SSE форматировании данные идут с префиксом "data: ", поэтому можно удалить этот префикс
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6); // Убираем "data: "
                        await received(data);
                    }
                }
            }
        }

        public async Task StreamAsAsync<T>(string url, object request,
            Func<T, Task> received,
            CancellationToken cancellationToken = default(CancellationToken),
            Action<HttpRequestMessage> useRequest = null)
        {
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{BaseUrl}{url}"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(request),
                    Encoding.UTF8, "application/json")
            };
            EnsureAuthentication(httpRequest);
            useRequest?.Invoke(httpRequest);

            using (var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8, false, bufferSize: 1);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data: "))
                    {
                        var text = line.Substring(6); // Убираем "data: "
                        var data = JsonConvert.DeserializeObject<T>(text);
                        //logger.LogWarning(text);
                        await received(data);
                    }
                }
            }
        }

        public async Task<T> SendAsAsync<T>(string url, object request,
            CancellationToken cancellationToken = default(CancellationToken),
            Action<HttpRequestMessage> useRequest = null)
        {
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{BaseUrl}{url}"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(request),
                    Encoding.UTF8, "application/json")
            };
            EnsureAuthentication(httpRequest);
            useRequest?.Invoke(httpRequest);

            using (var response = await httpClient.SendAsync(httpRequest, cancellationToken))
            {
                var s = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Service response: " + s);
                var result = await response.Content.ReadAsAsync<T>();
                return result;
            }
        }

        public async Task<string> SendAsStringAsync(string url, object request,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{BaseUrl}{url}"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(request),
                    Encoding.UTF8, "application/json"),
            };

            using (var response = await httpClient.SendAsync(httpRequest, cancellationToken))
            {
                var s = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Service response: " + s);
                return s;
            }
        }

        public async Task<T> SendAsXmlAsync<T>(string url, object request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var xml = request.SerializeAsXml();
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{BaseUrl}{url}"),
                Method = HttpMethod.Post,
                Content = new StringContent(xml, Encoding.UTF8, "text/xml")
            };
            EnsureAuthentication(httpRequest);

            using (var response = await httpClient.SendAsync(httpRequest, cancellationToken))
            {
                var s = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Service response: " + s);
                var result = s.DeserializeXml<T>();
                return result;
            }
        }

        public async Task<Stream> SendAsStreamAsync(string url, object request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = new Uri($"{BaseUrl}{url}"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(request),
                    Encoding.UTF8, "application/json")
            };
            EnsureAuthentication(httpRequest);

            using (var response = await httpClient.SendAsync(httpRequest, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var result = new MemoryStream();
                await stream.CopyToAsync(result);
                result.Position = 0;
                return result;
            }
        }

        private void EnsureAuthentication(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(AuthToken))
                AddAuthentication(request);
        }

        private void AddAuthentication(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken);
        }
    }
}
