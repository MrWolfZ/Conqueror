using System;
using Conqueror;
using Conqueror.CQS.Extensions.AspNetCore.Client;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsAspNetCoreClientServiceProviderExtensions
    {
        public static TCommandHandler CreateCommandHttpClient<TCommandHandler>(this IServiceProvider provider)
            where TCommandHandler : class, ICommandHandler
        {
            return provider.GetRequiredService<IConquerorHttpClientFactory>().CreateCommandHttpClient<TCommandHandler>();
        }

        public static TQueryHandler CreateQueryHttpClient<TQueryHandler>(this IServiceProvider provider)
            where TQueryHandler : class, IQueryHandler
        {
            return provider.GetRequiredService<IConquerorHttpClientFactory>().CreateQueryHttpClient<TQueryHandler>();
        }
    }
}
