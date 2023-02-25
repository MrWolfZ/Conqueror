using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class ConfigurationProvider : IDisposable
    {
        private readonly IReadOnlyCollection<Action<ConquerorCqsHttpClientGlobalOptions>> configureGlobalOptions;
        private readonly Lazy<HttpClient> httpClientLazy = new();

        public ConfigurationProvider(IEnumerable<Action<ConquerorCqsHttpClientGlobalOptions>> configureGlobalOptions)
        {
            this.configureGlobalOptions = configureGlobalOptions.ToList();
        }

        public void Dispose()
        {
            if (httpClientLazy.IsValueCreated)
            {
                httpClientLazy.Value.Dispose();
            }
        }

        public ResolvedHttpClientOptions GetOptions(IServiceProvider provider, HttpClientRegistration registration)
        {
            var commandOptions = new HttpCommandClientOptions(provider);
            registration.CommandConfigurationAction?.Invoke(commandOptions);

            var queryOptions = new HttpQueryClientOptions(provider);
            registration.QueryConfigurationAction?.Invoke(queryOptions);

            var globalOptions = new ConquerorCqsHttpClientGlobalOptions(provider);

            foreach (var configure in configureGlobalOptions)
            {
                configure(globalOptions);
            }

            var httpClient = CreateHttpClient(registration, globalOptions);

            var baseAddress = httpClient.BaseAddress ?? registration.BaseAddress;

            if (!baseAddress.IsAbsoluteUri)
            {
                throw new InvalidOperationException($"configuration error while creating options for Conqueror HTTP transport client: base address must be an absolute URI, but got '{baseAddress}'");
            }

            var jsonSerializerOptions = queryOptions.JsonSerializerOptions ?? commandOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;
            var commandPathConvention = commandOptions.PathConvention ?? globalOptions.CommandPathConvention;
            var queryPathConvention = queryOptions.PathConvention ?? globalOptions.QueryPathConvention;
            var headers = queryOptions.OptionalHeaders ?? commandOptions.OptionalHeaders;

            return new(httpClient, baseAddress, jsonSerializerOptions, commandPathConvention, queryPathConvention, headers);
        }

        private HttpClient CreateHttpClient(HttpClientRegistration registration, ConquerorCqsHttpClientGlobalOptions globalOptions)
        {
            if (registration.CommandType is not null && (globalOptions.CommandHttpClients?.TryGetValue(registration.CommandType, out var commandClient) ?? false))
            {
                return commandClient;
            }

            if (registration.QueryType is not null && (globalOptions.QueryHttpClients?.TryGetValue(registration.QueryType, out var queryClient) ?? false))
            {
                return queryClient;
            }

            return globalOptions.GlobalHttpClient ?? httpClientLazy.Value;
        }
    }
}
