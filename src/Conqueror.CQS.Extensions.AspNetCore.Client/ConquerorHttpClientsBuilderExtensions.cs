using System;
using System.Linq;
using Conqueror;
using Conqueror.CQS.Extensions.AspNetCore.Client;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorHttpClientsBuilderExtensions
    {
        public static IConquerorHttpClientsBuilder ConfigureDefaultHttpClientOptions(this IConquerorHttpClientsBuilder builder,
                                                                                     Action<ConquerorHttpClientOptions> configure)
        {
            var existingOptions = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(ConquerorHttpClientOptions))?.ImplementationInstance;
            if (existingOptions is ConquerorHttpClientOptions { HandlerType: null })
            {
                throw new InvalidOperationException("default http client options have already been registered");
            }

            var options = new ConquerorHttpClientOptions();
            configure(options);
            _ = builder.Services.AddSingleton(options);

            return builder;
        }

        public static IConquerorHttpClientsBuilder AddCommandHttpClient<TCommandHandler>(this IConquerorHttpClientsBuilder builder,
                                                                                         Action<ConquerorHttpClientOptions>? configure = null)
            where TCommandHandler : class, ICommandHandler
        {
            var (commandType, _) = typeof(TCommandHandler).GetCommandAndResponseTypes().Single();
            commandType.AssertCommandIsHttpCommand();

            var plainHandlerInterfaceType = typeof(TCommandHandler).GetCommandHandlerInterfaceTypes().Single();

            if (builder.Services.Any(d => d.ServiceType == plainHandlerInterfaceType))
            {
                throw new InvalidOperationException($"an http client for command handler {typeof(TCommandHandler).FullName} is already registered");
            }

            if (configure is not null)
            {
                var options = new ConquerorHttpClientOptions { HandlerType = typeof(TCommandHandler) };
                configure(options);
                _ = builder.Services.AddSingleton(options);
            }

            _ = builder.Services.AddTransient(p => p.CreateCommandHttpClient<TCommandHandler>());

            if (typeof(TCommandHandler).IsCustomCommandHandlerInterfaceType())
            {
                _ = builder.Services.AddTransient(plainHandlerInterfaceType, p => p.GetRequiredService<TCommandHandler>());
            }

            return builder;
        }

        public static IConquerorHttpClientsBuilder AddQueryHttpClient<TQueryHandler>(this IConquerorHttpClientsBuilder builder,
                                                                                     Action<ConquerorHttpClientOptions>? configure = null)
            where TQueryHandler : class, IQueryHandler
        {
            var (queryType, _) = typeof(TQueryHandler).GetQueryAndResponseTypes().Single();
            queryType.AssertQueryIsHttpQuery();

            var plainHandlerInterfaceType = typeof(TQueryHandler).GetQueryHandlerInterfaceTypes().Single();

            if (builder.Services.Any(d => d.ServiceType == plainHandlerInterfaceType))
            {
                throw new InvalidOperationException($"an http client for query handler {typeof(TQueryHandler).FullName} is already registered");
            }

            if (configure is not null)
            {
                var options = new ConquerorHttpClientOptions { HandlerType = typeof(TQueryHandler) };
                configure(options);
                _ = builder.Services.AddSingleton(options);
            }

            _ = builder.Services.AddTransient(p => p.CreateQueryHttpClient<TQueryHandler>());

            if (typeof(TQueryHandler).IsCustomQueryHandlerInterfaceType())
            {
                _ = builder.Services.AddTransient(plainHandlerInterfaceType, p => p.GetRequiredService<TQueryHandler>());
            }

            return builder;
        }
    }
}
