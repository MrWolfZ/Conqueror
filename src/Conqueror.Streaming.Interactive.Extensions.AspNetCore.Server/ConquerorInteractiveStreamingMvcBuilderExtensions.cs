using Conqueror.Streaming.Interactive.Extensions.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorInteractiveStreamingMvcBuilderExtensions
    {
        public static IMvcBuilder AddConquerorInteractiveStreaming(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton(new InteractiveStreamingAspNetCoreServerServiceCollectionConfigurator());

            return builder;
        }
    }
}
