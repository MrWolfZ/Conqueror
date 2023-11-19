using System;
using Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingTransportWebSocketsServerAspNetCoreMvcBuilderExtensions
{
    public static IMvcBuilder AddConquerorEventingWebSocketsControllers(this IMvcBuilder builder, Action<ConquerorEventingWebSocketsTransportServerAspNetCoreOptions>? configureOptions = null)
    {
        _ = builder.Services.AddConquerorEventing().AddConquerorEventingWebSocketsTransportPublisher().AddHttpContextAccessor();

        builder.Services.TryAddSingleton<HttpEndpointRegistry>();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, HttpEndpointConfigurationStartupFilter>());

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()));

        var options = new ConquerorEventingWebSocketsTransportServerAspNetCoreOptions();

        configureOptions?.Invoke(options);

        _ = builder.Services.AddSingleton(options);

        return builder;
    }
}
