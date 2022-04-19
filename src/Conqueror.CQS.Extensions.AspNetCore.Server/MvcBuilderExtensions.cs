using Conqueror.CQS.Extensions.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddConqueror(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton(new AspNetCoreServerServiceCollectionConfigurator());

            return builder;
        }
    }
}
