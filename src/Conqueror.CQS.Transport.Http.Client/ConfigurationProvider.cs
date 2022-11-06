﻿using System;

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

            var clientOptions = new ConquerorCqsHttpClientOptions(provider);
            registration.ConfigurationAction?.Invoke(clientOptions);

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

            var jsonSerializerOptions = clientOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;

            return new(httpClient, jsonSerializerOptions);
        }
    }
}