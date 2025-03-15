using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsTransportHttpServerAspNetCoreMvcBuilderExtensions
{
    [RequiresDynamicCode("Needs to create generic types at runtime")]
    public static IMvcBuilder AddConquerorMessageControllers(this IMvcBuilder builder)
    {
        _ = builder.Services.AddConqueror();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, HttpEndpointReflectionConfigurationStartupFilter>());

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()));

        return builder;
    }

    public static IMvcBuilder AddConquerorMessageController<TMessage>(this IMvcBuilder builder)
        where TMessage : class, IHttpMessage<TMessage>
    {
        _ = builder.Services.AddConqueror();

        _ = builder.Services.AddSingleton(TMessage.CreateWithMessageTypes(new ControllerTypeInjectionFactory()));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, HttpEndpointConfigurationStartupFilter>());

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()));

        return builder;
    }

    private sealed class ControllerTypeInjectionFactory : IHttpMessageTypesInjectionFactory<HttpMessageControllerRegistration>
    {
        public HttpMessageControllerRegistration Create<TMessage, TResponse>()
            where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage>
            => new(typeof(MessageApiController<TMessage, TResponse>).GetTypeInfo());
    }
}
