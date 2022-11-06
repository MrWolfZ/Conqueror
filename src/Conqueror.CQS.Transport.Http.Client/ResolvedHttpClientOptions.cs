using System.Net.Http;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class ResolvedHttpClientOptions
    {
        public ResolvedHttpClientOptions(HttpClient httpClient, JsonSerializerOptions? jsonSerializerOptions)
        {
            HttpClient = httpClient;
            JsonSerializerOptions = jsonSerializerOptions;
        }

        public HttpClient HttpClient { get; }

        public JsonSerializerOptions? JsonSerializerOptions { get; }
    }
}
