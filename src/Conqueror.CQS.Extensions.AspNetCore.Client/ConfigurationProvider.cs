using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class ConfigurationProvider
    {
        private readonly Action<ConquerorCqsHttpClientGlobalOptions>? configureGlobalOptions;
        private readonly IReadOnlyDictionary<Type, HttpClientRegistration> registrationsByHandlerType;

        public ConfigurationProvider(IEnumerable<HttpClientRegistration> clientRegistrations,
                                     Action<ConquerorCqsHttpClientGlobalOptions>? configureGlobalOptions = null)
        {
            this.configureGlobalOptions = configureGlobalOptions;
            registrationsByHandlerType = clientRegistrations.ToDictionary(o => o.HandlerType);
        }

        public ResolvedHttpClientOptions GetOptions<THandler>(IServiceProvider provider)
        {
            var registration = registrationsByHandlerType.TryGetValue(typeof(THandler), out var o)
                ? o
                : throw new InvalidOperationException($"http handler '{typeof(THandler).Name}' is not registered");

            var globalOptions = new ConquerorCqsHttpClientGlobalOptions(provider);
            configureGlobalOptions?.Invoke(globalOptions);

            var clientOptions = new ConquerorCqsHttpClientOptions(provider);
            registration.ConfigurationAction?.Invoke(clientOptions);

            var httpClient = registration.HttpClientFactory?.Invoke(provider);

            if (httpClient is null)
            {
                if (registration.BaseAddressFactory is null)
                {
                    throw new InvalidOperationException(
                        $"configuration error while creating options for http handler '{typeof(THandler).Name}': either http client or base address factory must be provided");
                }
                
                var baseAddress = registration.BaseAddressFactory(provider);
                httpClient = globalOptions.HttpClientFactory(baseAddress);
            }

            var jsonSerializerOptions = clientOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;

            return new(httpClient, jsonSerializerOptions);
        }
    }
}
