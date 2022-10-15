using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Conqueror.Common;
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

            var registration = new HttpClientRegistration(typeof(TQueryHandler))
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

        public TCommandHandler CreateCommandHttpClient<TCommandHandler>(IServiceProvider serviceProvider,
                                                                        Func<IServiceProvider, HttpClient>? httpClientFactory = null,
                                                                        Func<IServiceProvider, Uri>? baseAddressFactory = null,
                                                                        Action<ConquerorCqsHttpClientOptions>? configureOptions = null)
            where TCommandHandler : class, ICommandHandler
        {
            var (commandType, responseType) = typeof(TCommandHandler).GetCommandAndResponseTypes().Single();
            commandType.AssertCommandIsHttpCommand();

            var registration = new HttpClientRegistration(typeof(TCommandHandler))
            {
                HttpClientFactory = httpClientFactory,
                BaseAddressFactory = baseAddressFactory,
                ConfigurationAction = configureOptions,
            };

            var result = GetMethod()?.Invoke(this, new object?[] { serviceProvider, registration });
            return result as TCommandHandler ?? throw new InvalidOperationException($"could not created typed query client for handler type {typeof(TCommandHandler).Name}");

            MethodInfo? GetMethod()
            {
                if (responseType == null)
                {
                    var m = typeof(HttpClientFactory).GetMethod(nameof(CreateTypedCommandWithoutResponseClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
                    return m?.MakeGenericMethod(typeof(TCommandHandler), commandType);
                }

                var method = typeof(HttpClientFactory).GetMethod(nameof(CreateTypedCommandClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
                return method?.MakeGenericMethod(typeof(TCommandHandler), commandType, responseType);
            }
        }

        private object? CreateTypedCommandClientGeneric<TCommandHandler, TCommand, TResponse>(IServiceProvider serviceProvider, HttpClientRegistration registration)
            where TCommandHandler : class, ICommandHandler
            where TCommand : class
        {
            var handler = new HttpCommandHandler<TCommand, TResponse>(configurationProvider.GetOptions<TCommandHandler>(serviceProvider, registration),
                                                                      serviceProvider.GetService<IConquerorContextAccessor>());

            if (!typeof(TCommandHandler).IsCustomCommandHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TCommandHandler), typeof(ICommandHandler<TCommand, TResponse>));
            return Activator.CreateInstance(dynamicType, handler);
        }

        private object? CreateTypedCommandWithoutResponseClientGeneric<TCommandHandler, TCommand>(IServiceProvider serviceProvider, HttpClientRegistration registration)
            where TCommandHandler : class, ICommandHandler
            where TCommand : class
        {
            var handler = new HttpCommandHandler<TCommand>(configurationProvider.GetOptions<TCommandHandler>(serviceProvider, registration),
                                                           serviceProvider.GetService<IConquerorContextAccessor>());

            if (!typeof(TCommandHandler).IsCustomCommandHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TCommandHandler), typeof(ICommandHandler<TCommand>));
            return Activator.CreateInstance(dynamicType, handler);
        }

        private object? CreateTypedQueryClientGeneric<TQueryHandler, TQuery, TResponse>(IServiceProvider serviceProvider, HttpClientRegistration registration)
            where TQueryHandler : class, IQueryHandler
            where TQuery : class
        {
            var handler = new HttpQueryHandler<TQuery, TResponse>(configurationProvider.GetOptions<TQueryHandler>(serviceProvider, registration),
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
