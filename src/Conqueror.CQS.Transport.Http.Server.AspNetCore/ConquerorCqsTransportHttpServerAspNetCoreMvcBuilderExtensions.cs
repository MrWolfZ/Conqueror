using System;
using Conqueror.CQS.Transport.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsTransportHttpServerAspNetCoreMvcBuilderExtensions
{
    public static IMvcBuilder AddConquerorCQSHttpControllers(this IMvcBuilder builder, Action<ConquerorCqsHttpTransportServerAspNetCoreOptions>? configureOptions = null)
    {
        _ = builder.Services.AddConquerorCQS().AddHttpContextAccessor();

        builder.Services.TryAddSingleton<HttpEndpointRegistry>();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, HttpEndpointConfigurationStartupFilter>());

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()));

        var options = new ConquerorCqsHttpTransportServerAspNetCoreOptions();

        configureOptions?.Invoke(options);

        _ = builder.Services.AddSingleton(options);

        return builder;
    }
}
