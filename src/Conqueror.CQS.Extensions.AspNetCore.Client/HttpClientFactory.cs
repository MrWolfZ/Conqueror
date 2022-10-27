using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Conqueror.Common;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class HttpClientFactory
    {
        private readonly ConfigurationProvider configurationProvider;

        public HttpClientFactory(ConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public TQueryHandler CreateQueryHttpClient<TQueryHandler>(IServiceProvider serviceProvider,
                                                                  Func<IServiceProvider, HttpClient>? httpClientFactory = null,
                                                                  Func<IServiceProvider, Uri>? baseAddressFactory = null,
                                                                  Action<ConquerorCqsHttpClientOptions>? configureOptions = null)
            where TQueryHandler : class, IQueryHandler
        {
            var (queryType, responseType) = typeof(TQueryHandler).GetQueryAndResponseTypes().Single();
            queryType.AssertQueryIsHttpQuery();

            var registration = new HttpClientRegistration
            {
                HttpClientFactory = httpClientFactory,
                BaseAddressFactory = baseAddressFactory,
                ConfigurationAction = configureOptions,
            };

            var method = typeof(HttpClientFactory).GetMethod(nameof(CreateTypedQueryClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
            var typedMethod = method?.MakeGenericMethod(typeof(TQueryHandler), queryType, responseType);
            var result = typedMethod?.Invoke(this, new object?[] { serviceProvider, registration });
            return result as TQueryHandler ?? throw new InvalidOperationException($"could not created typed query client for handler type {typeof(TQueryHandler).Name}");
        }

        private object? CreateTypedQueryClientGeneric<TQueryHandler, TQuery, TResponse>(IServiceProvider serviceProvider, HttpClientRegistration registration)
            where TQueryHandler : class, IQueryHandler
            where TQuery : class
        {
            var handler = new HttpQueryHandler<TQuery, TResponse>(configurationProvider.GetOptions(serviceProvider, registration),
                                                                  serviceProvider.GetService<IConquerorContextAccessor>());

            if (!typeof(TQueryHandler).IsCustomQueryHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TQueryHandler), typeof(IQueryHandler<TQuery, TResponse>));
            return Activator.CreateInstance(dynamicType, handler);
        }
    }
}
