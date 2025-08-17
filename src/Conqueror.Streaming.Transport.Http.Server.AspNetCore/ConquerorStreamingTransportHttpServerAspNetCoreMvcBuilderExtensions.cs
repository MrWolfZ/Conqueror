#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using AspNetCore.Hosting;
using AspNetCore.Mvc.Infrastructure;
using Conqueror.Streaming.Transport.Http.Server.AspNetCore;
using Extensions;

public static class ConquerorStreamingTransportHttpServerAspNetCoreMvcBuilderExtensions
{
    public static IMvcBuilder AddConquerorStreamingHttpControllers(
        this IMvcBuilder builder,
        Action<ConquerorStreamingHttpTransportServerAspNetCoreOptions>? configureOptions = null
    )
    {
        _ = builder.Services.AddConquerorStreaming().AddHttpContextAccessor();

        builder.Services.TryAddSingleton<HttpEndpointRegistry>();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IStartupFilter, HttpEndpointConfigurationStartupFilter>()
        );

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()
            )
        );

        var options = new ConquerorStreamingHttpTransportServerAspNetCoreOptions();

        configureOptions?.Invoke(options);

        _ = builder.Services.AddSingleton(options);

        return builder;
    }
}
