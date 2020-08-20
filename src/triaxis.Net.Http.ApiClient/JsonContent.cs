using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace triaxis.Net.Http
{
    internal class JsonContent<T> : HttpContent
    {
        private readonly T _content;
        private readonly JsonSerializerOptions _options;

        public JsonContent(T content, JsonSerializerOptions options)
        {
            _content = content;
            _options = options;

            Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = System.Text.Encoding.UTF8.WebName };
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => _content == null ? Task.CompletedTask : JsonSerializer.SerializeAsync(stream, _content, _options);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return _content == null;
        }
    }
}
