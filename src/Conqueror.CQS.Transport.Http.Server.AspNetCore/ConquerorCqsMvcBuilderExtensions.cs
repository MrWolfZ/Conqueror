using Conqueror.CQS.Transport.Http.Server.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsMvcBuilderExtensions
    {
        public static IMvcBuilder AddConquerorCQSHttpControllers(this IMvcBuilder builder)
        {
            _ = builder.Services.AddFinalizationCheck();
            
            builder.Services.TryAddSingleton(new CqsAspNetCoreServerServiceCollectionConfigurator());

            return builder;
        }
    }
}
