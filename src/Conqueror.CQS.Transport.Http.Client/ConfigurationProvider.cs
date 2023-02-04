using System;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class ConfigurationProvider
    {
        private readonly Action<ConquerorCqsHttpClientGlobalOptions>? configureGlobalOptions;

        public ConfigurationProvider(Action<ConquerorCqsHttpClientGlobalOptions>? configureGlobalOptions = null)
        {
            this.configureGlobalOptions = configureGlobalOptions;
        }

        public ResolvedHttpClientOptions GetOptions(IServiceProvider provider, HttpClientRegistration registration)
        {
            var globalOptions = new ConquerorCqsHttpClientGlobalOptions(provider);
            configureGlobalOptions?.Invoke(globalOptions);

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
