using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class ConfigurationProvider
    {
        private readonly IReadOnlyDictionary<Type, ConquerorHttpClientOptions> optionsByHandlerType;

        public ConfigurationProvider(IEnumerable<ConquerorHttpClientOptions> options)
        {
            optionsByHandlerType = options.ToDictionary(o => o.HandlerType ?? typeof(ConfigurationProvider));
        }

        public ResolvedHttpClientOptions GetOptions<THandler>(IServiceProvider provider)
        {
            var handlerOptions = optionsByHandlerType.TryGetValue(typeof(THandler), out var o) ? o : null;
            var defaultOptions = optionsByHandlerType.TryGetValue(typeof(ConfigurationProvider), out var d) ? d : null;

            var httpClientFactory = handlerOptions?.HttpClientFactory ?? defaultOptions?.HttpClientFactory ?? (_ => new HttpClient());
            var httpClient = httpClientFactory(provider);

            var jsonSerializerOptionsFactory = handlerOptions?.JsonSerializerOptionsFactory ?? defaultOptions?.JsonSerializerOptionsFactory;
            var jsonSerializerOptions = jsonSerializerOptionsFactory?.Invoke(provider);

            return new(httpClient, jsonSerializerOptions);
        }
    }
}
