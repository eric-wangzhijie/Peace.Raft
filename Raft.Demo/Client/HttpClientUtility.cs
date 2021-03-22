using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Raft.Demo
{  
    public class HttpClientUtility
    {
        private IHttpClientFactory _httpClientFactory;

        public HttpClientUtility(IHttpClientFactory httpClientFactory)
        {
            this._httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetAsync(string url, Dictionary<string, string> dicHeaders, int timeoutSecond = 60)
        {
            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (dicHeaders != null)
            {
                foreach (var header in dicHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            client.Timeout = TimeSpan.FromSeconds(timeoutSecond);
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> PostAsync(string url, string requestString, Dictionary<string, string> dicHeaders, int timeoutSecond)
        {
            using var client = _httpClientFactory.CreateClient();
            var requestContent = new StringContent(requestString);
            if (dicHeaders != null)
            {
                foreach (var head in dicHeaders)
                {
                    requestContent.Headers.Add(head.Key, head.Value);
                }
            }
            client.Timeout = TimeSpan.FromSeconds(timeoutSecond);
            var response = await client.PostAsync(url, requestContent);

            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> PutAsync(string url, string requestString, Dictionary<string, string> dicHeaders, int timeoutSecond)
        {
            using var client = _httpClientFactory.CreateClient();
            var requestContent = new StringContent(requestString);
            if (dicHeaders != null)
            {
                foreach (var head in dicHeaders)
                {
                    requestContent.Headers.Add(head.Key, head.Value);
                }
            }
            client.Timeout = TimeSpan.FromSeconds(timeoutSecond);
            var response = await client.PutAsync(url, requestContent);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> PatchAsync(string url, string requestString, Dictionary<string, string> dicHeaders, int timeoutSecond)
        {
            using var client = _httpClientFactory.CreateClient();
            var requestContent = new StringContent(requestString);
            if (dicHeaders != null)
            {
                foreach (var head in dicHeaders)
                {
                    requestContent.Headers.Add(head.Key, head.Value);
                }
            }
            client.Timeout = TimeSpan.FromSeconds(timeoutSecond);
            var response = await client.PatchAsync(url, requestContent);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> DeleteAsync(string url, Dictionary<string, string> dicHeaders, int timeoutSecond)
        {
            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            if (dicHeaders != null)
            {
                foreach (var head in dicHeaders)
                {
                    request.Headers.Add(head.Key, head.Value);
                }
            }
            client.Timeout = TimeSpan.FromSeconds(timeoutSecond);
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> ExecuteAsync(string url, HttpMethod method, string requestString, Dictionary<string, string> dicHeaders, int timeoutSecond = 60)
        {
            using var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(requestString),
            };
            if (dicHeaders != null)
            {
                foreach (var header in dicHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
