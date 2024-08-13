using Conqueror.Streaming.Transport.Http.Server.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingMvcBuilderExtensions
{
    public static IMvcBuilder AddConquerorStreaming(this IMvcBuilder builder)
    {
        _ = builder.Services.AddConquerorStreaming();

        builder.Services.TryAddSingleton(new StreamingHttpServerAspNetCoreRegistrationFinalizer(builder.Services));

        return builder;
    }
}
