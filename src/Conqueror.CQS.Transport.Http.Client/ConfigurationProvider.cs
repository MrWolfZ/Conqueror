using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class ConfigurationProvider : IDisposable
    {
        private readonly IReadOnlyCollection<Action<ConquerorCqsHttpClientGlobalOptions>> configureGlobalOptions;
        private readonly Lazy<HttpClient> httpClientLazy = new();
        private readonly ConcurrentDictionary<Type, bool> httpValidityByRequestType = new();

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
            ValidateRequestType(registration);

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

            var requestType = registration.CommandType ?? registration.QueryType;

            if (requestType is not null && (globalOptions.AssemblyClients?.TryGetValue(requestType.Assembly, out var assemblyClient) ?? false))
            {
                return assemblyClient;
            }

            return globalOptions.GlobalHttpClient ?? httpClientLazy.Value;
        }

        private void ValidateRequestType(HttpClientRegistration registration)
        {
            if (registration.CommandType is { } commandType)
            {
                var isHttpCommand = httpValidityByRequestType.GetOrAdd(commandType, t => t.GetCustomAttribute<HttpCommandAttribute>() != null);

                if (!isHttpCommand)
                {
                    throw new InvalidOperationException($"command type '{commandType.Name}' is not an HTTP command; did you forget to add the '[HttpCommand]' attribute?");
                }
            }

            if (registration.QueryType is { } queryType)
            {
                var isHttpQuery = httpValidityByRequestType.GetOrAdd(queryType, t => t.GetCustomAttribute<HttpQueryAttribute>() != null);

                if (!isHttpQuery)
                {
                    throw new InvalidOperationException($"query type '{queryType.Name}' is not an HTTP query; did you forget to add the '[HttpQuery]' attribute?");
                }
            }
        }
    }
}
