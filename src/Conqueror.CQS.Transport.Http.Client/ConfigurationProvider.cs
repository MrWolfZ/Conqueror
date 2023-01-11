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

            var httpClient = registration.HttpClientFactory?.Invoke(provider);

            if (httpClient is null)
            {
                if (registration.BaseAddressFactory is null)
                {
                    throw new InvalidOperationException("configuration error while creating options for http handler: either http client or base address factory must be provided");
                }

                var baseAddress = registration.BaseAddressFactory(provider);
                httpClient = globalOptions.HttpClientFactory(baseAddress);
            }

            var jsonSerializerOptions = queryOptions.JsonSerializerOptions ?? commandOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;
            var commandPathConvention = commandOptions.PathConvention ?? globalOptions.CommandPathConvention;
            var queryPathConvention = queryOptions.PathConvention ?? globalOptions.QueryPathConvention;
            var headers = queryOptions.OptionalHeaders ?? commandOptions.OptionalHeaders;

            return new(httpClient, jsonSerializerOptions, commandPathConvention, queryPathConvention, headers);
        }
    }
}
