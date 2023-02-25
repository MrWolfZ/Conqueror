using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class ConfigurationProvider
    {
        private readonly IReadOnlyCollection<Action<ConquerorCqsHttpClientGlobalOptions>> configureGlobalOptions;

        public ConfigurationProvider(IEnumerable<Action<ConquerorCqsHttpClientGlobalOptions>> configureGlobalOptions)
        {
            this.configureGlobalOptions = configureGlobalOptions.ToList();
        }

        public ResolvedHttpClientOptions GetOptions(IServiceProvider provider, HttpClientRegistration registration)
        {
            var globalOptions = new ConquerorCqsHttpClientGlobalOptions(provider);

            foreach (var configure in configureGlobalOptions)
            {
                configure(globalOptions);
            }

            var commandOptions = new HttpCommandClientOptions(provider);
            registration.CommandConfigurationAction?.Invoke(commandOptions);

            var queryOptions = new HttpQueryClientOptions(provider);
            registration.QueryConfigurationAction?.Invoke(queryOptions);

            var httpClient = registration.HttpClient;

            if (httpClient is null)
            {
                if (registration.BaseAddress is null)
                {
                    // should not be possible with public API, since it enforces either one to be provided
                    throw new InvalidOperationException("configuration error while creating options for Conqueror HTTP transport client: either HTTP client or base address must be provided");
                }

                httpClient = globalOptions.HttpClientFactory(registration.BaseAddress);
            }

            if (httpClient.BaseAddress is null)
            {
                throw new InvalidOperationException("configuration error while creating options for Conqueror HTTP transport client: HTTP client base address is not set");
            }

            var jsonSerializerOptions = queryOptions.JsonSerializerOptions ?? commandOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;
            var commandPathConvention = commandOptions.PathConvention ?? globalOptions.CommandPathConvention;
            var queryPathConvention = queryOptions.PathConvention ?? globalOptions.QueryPathConvention;
            var headers = queryOptions.OptionalHeaders ?? commandOptions.OptionalHeaders;

            return new(httpClient, jsonSerializerOptions, commandPathConvention, queryPathConvention, headers);
        }
    }
}
