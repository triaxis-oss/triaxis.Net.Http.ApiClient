using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace triaxis.Net.Http
{
    /// <summary>
    /// Helper for creating simple API proxies
    /// </summary>
    public class ApiClient : HttpClient
    {
        /// <summary>
        /// Shared <see cref="HttpClientHandler" /> used by default
        /// to avoid the dispose issues with <see cref="HttpClient" /> instances
        /// </summary>
        public static HttpClientHandler SharedHandler { get; } = new HttpClientHandler();

        // default options
        private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
        };

        /// <summary>
        /// Creates an <see cref="ApiClient" /> instance, using the shared <see cref="HttpClientHandler" /> instance
        /// </summary>
        public ApiClient()
            : base(SharedHandler, false)
        {
        }

        /// <summary>
        /// Creates an <see cref="ApiClient" /> instance, using the provided <see cref="HttpMessageHandler" /> instance
        /// </summary>
        public ApiClient(HttpMessageHandler handler, bool disposeHandler = true)
            : base(handler, disposeHandler)
        {
        }

        /// <summary>
        /// Optional logger for request diagnostics
        /// </summary>
        protected ILogger Logger { get; set; }
        /// <summary>
        /// JSON serialization options
        /// </summary>
        protected JsonSerializerOptions SerializerOptions { get; set; } = s_defaultOptions;

        /// <summary>
        /// Retrieves an object of the specified type from the specified path
        /// </summary>
        public Task<T> GetAsync<T>(string query)
            => ExecuteAsync<T>(HttpMethod.Get, query);
        /// <summary>
        /// Posts an object of the specified type to the specified path
        /// </summary>
        public Task PostAsync<T>(string query, T content)
            => ExecuteAsync<object, T>(HttpMethod.Post, query, content);
        /// <summary>
        /// Puts an object of the specified type to the specified path
        /// </summary>
        public Task PutAsync<T>(string query, T content)
            => ExecuteAsync<object, T>(HttpMethod.Put, query, content);

        /// <summary>
        /// Formats a query string from the interpolated <see cref="FormattableString" />
        /// </summary>
        protected static string FormatQuery(FormattableString query)
            => string.Format(query.Format, Array.ConvertAll(query.GetArguments(), arg => HttpUtility.UrlEncode(arg?.ToString() ?? "")));
        /// <summary>
        /// Formats a query string from the interpolated <see cref="FormattableString" />,
        /// appending the specified arguments
        /// </summary>
        protected static string FormatQuery<T>(FormattableString query, T args)
            => FormatQuery(FormatQuery(query), args);
        /// <summary>
        /// Formats a query string from the string, appending the specified arguments
        /// </summary>
        protected static string FormatQuery<T>(string query, T args)
            => QueryStringFormatter<T>.Format(query, args);

        /// <summary>
        /// Executes the specified query using the specified method, deserializing the result
        /// </summary>
        public Task<T> ExecuteAsync<T>(HttpMethod method, string query)
            => ExecuteAsync<T, object>(method, query, null);
        /// <summary>
        /// Executes the specified query using the specified method with the provided content, deserializing the result
        /// </summary>
        public async Task<TResult> ExecuteAsync<TResult, TContent>(HttpMethod method, string query, TContent content)
        {
            Logger?.LogDebug(">> {Method} {BaseAddress}{Request}", method, BaseAddress, query);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var request = new HttpRequestMessage(method, query);
            if (content != null || typeof(TContent) != typeof(object))
            {
                // always set content, some methods fail without Content-Type set
                request.Content = content as HttpContent ?? new JsonContent<TContent>(content, SerializerOptions);
            }
            using var response = await SendAsync(request);
            using var stream = await response.Content.ReadAsStreamAsync();
            sw.Stop();

            Logger?.LogDebug("<< {StatusCode} in {Duration} ms", response.StatusCode, sw.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0)
                {
                    // empty content deserializes as deafult value, JsonSerializer would crash
                    return default;
                }
                return await JsonSerializer.DeserializeAsync<TResult>(stream, SerializerOptions);
            }

            return await OnErrorAsync<TResult>(response);
        }

        /// <summary>
        /// Callback for handling unsuccessful query responses
        /// </summary>
        /// <remarks>
        /// Default implementation throws an exception
        /// </remarks>
        protected virtual Task<TResult> OnErrorAsync<TResult>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            return Task.FromResult<TResult>(default);
        }
    }
}
