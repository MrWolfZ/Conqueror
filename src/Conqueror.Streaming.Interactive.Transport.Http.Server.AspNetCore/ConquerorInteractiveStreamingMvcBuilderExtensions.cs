using Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorInteractiveStreamingMvcBuilderExtensions
    {
        public static IMvcBuilder AddConquerorInteractiveStreaming(this IMvcBuilder builder)
        {
            _ = builder.Services.AddConquerorInteractiveStreaming();

            builder.Services.TryAddSingleton(new InteractiveStreamingHttpServerAspNetCoreRegistrationFinalizer(builder.Services));

            return builder;
        }
    }
}
