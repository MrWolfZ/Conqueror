using System;
using System.Reflection;
using Conqueror.Common;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class ConquerorHttpClientFactory : IConquerorHttpClientFactory
    {
        private readonly ConfigurationProvider configurationProvider;
        private readonly IServiceProvider serviceProvider;

        public ConquerorHttpClientFactory(ConfigurationProvider configurationProvider, IServiceProvider serviceProvider)
        {
            this.configurationProvider = configurationProvider;
            this.serviceProvider = serviceProvider;
        }

        public TQueryHandler CreateQueryHttpClient<TQueryHandler>()
            where TQueryHandler : class, IQueryHandler
        {
            var (queryType, responseType) = typeof(TQueryHandler).GetQueryAndResponseType();
            queryType.AssertQueryIsHttpQuery();

            var method = typeof(ConquerorHttpClientFactory).GetMethod(nameof(CreateTypedQueryClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
            var typedMethod = method?.MakeGenericMethod(typeof(TQueryHandler), queryType, responseType);
            var result = typedMethod?.Invoke(this, Array.Empty<object>());
            return result as TQueryHandler ?? throw new InvalidOperationException($"could not created typed query client for handler type {typeof(TQueryHandler).Name}");
        }

        public TCommandHandler CreateCommandHttpClient<TCommandHandler>()
            where TCommandHandler : class, ICommandHandler
        {
            var (commandType, responseType) = typeof(TCommandHandler).GetCommandAndResponseType();
            commandType.AssertCommandIsHttpCommand();

            var result = GetMethod()?.Invoke(this, Array.Empty<object>());
            return result as TCommandHandler ?? throw new InvalidOperationException($"could not created typed query client for handler type {typeof(TCommandHandler).Name}");

            MethodInfo? GetMethod()
            {
                if (responseType == null)
                {
                    var m = typeof(ConquerorHttpClientFactory).GetMethod(nameof(CreateTypedCommandWithoutResponseClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
                    return m?.MakeGenericMethod(typeof(TCommandHandler), commandType);
                }

                var method = typeof(ConquerorHttpClientFactory).GetMethod(nameof(CreateTypedCommandClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
                return method?.MakeGenericMethod(typeof(TCommandHandler), commandType, responseType);
            }
        }

        private object? CreateTypedCommandClientGeneric<TCommandHandler, TCommand, TResponse>()
            where TCommandHandler : class, ICommandHandler
            where TCommand : class
        {
            var handler = new HttpCommandHandler<TCommand, TResponse>(configurationProvider.GetOptions<TCommandHandler>(serviceProvider));

            if (!typeof(TCommandHandler).IsCustomCommandHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TCommandHandler), typeof(ICommandHandler<TCommand, TResponse>));
            return Activator.CreateInstance(dynamicType, handler);
        }

        private object? CreateTypedCommandWithoutResponseClientGeneric<TCommandHandler, TCommand>()
            where TCommandHandler : class, ICommandHandler
            where TCommand : class
        {
            var handler = new HttpCommandHandler<TCommand>(configurationProvider.GetOptions<TCommandHandler>(serviceProvider));

            if (!typeof(TCommandHandler).IsCustomCommandHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TCommandHandler), typeof(ICommandHandler<TCommand>));
            return Activator.CreateInstance(dynamicType, handler);
        }

        private object? CreateTypedQueryClientGeneric<TQueryHandler, TQuery, TResponse>()
            where TQueryHandler : class, IQueryHandler
            where TQuery : class
        {
            var handler = new HttpQueryHandler<TQuery, TResponse>(configurationProvider.GetOptions<TQueryHandler>(serviceProvider));

            if (!typeof(TQueryHandler).IsCustomQueryHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TQueryHandler), typeof(IQueryHandler<TQuery, TResponse>));
            return Activator.CreateInstance(dynamicType, handler);
        }
    }
}
