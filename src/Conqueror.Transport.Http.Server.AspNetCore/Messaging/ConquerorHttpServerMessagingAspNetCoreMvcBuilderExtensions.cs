using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerMessagingAspNetCoreMvcBuilderExtensions
{
    public static IMvcBuilder AddConquerorMessageControllers(this IMvcBuilder builder)
    {
        _ = builder.Services.AddConqueror();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, HttpMessageEndpointReflectionConfigurationStartupFilter>());

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, HttpMessageEndpointReflectionApplicationModelProvider>());
        builder.Services.TryAddTransient(typeof(HttpMessageEndpointApplicationModelProvider<,>));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, HttpMessageDuplicatePathValidationApplicationModelProvider>());

        return builder;
    }

    public static IMvcBuilder AddConquerorMessageController<TMessage>(this IMvcBuilder builder)
        where TMessage : class, IHttpMessage
    {
        _ = builder.Services.AddConqueror();

        _ = builder.Services.AddSingleton(TMessage.HttpMessageTypesInjector.CreateWithMessageTypes(new ControllerRegistrationTypeInjector()));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, HttpMessageEndpointConfigurationStartupFilter>());

        builder.Services.TryAddSingleton<HttpEndpointActionDescriptorChangeProvider>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HttpEndpointActionDescriptorChangeProvider>(
                p => p.GetRequiredService<HttpEndpointActionDescriptorChangeProvider>()));

        _ = TMessage.HttpMessageTypesInjector.CreateWithMessageTypes(new ApplicationModelProviderTypeInjectionFactory(builder.Services));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, HttpMessageDuplicatePathValidationApplicationModelProvider>());

        return builder;
    }

    private sealed class ControllerRegistrationTypeInjector : IHttpMessageTypesInjector<HttpMessageControllerRegistration>
    {
        public HttpMessageControllerRegistration WithInjectedTypes<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            TMessage,
            TResponse>()
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            if (TMessage.EmptyInstance is not null)
            {
                return new(typeof(MessageApiControllerWithoutPayload<TMessage, TResponse>).GetTypeInfo());
            }

            if (TMessage.HttpMethod == ConquerorTransportHttpConstants.MethodGet)
            {
                return new(typeof(MessageApiControllerForGet<TMessage, TResponse>).GetTypeInfo());
            }

            return new(typeof(MessageApiController<TMessage, TResponse>).GetTypeInfo());
        }
    }

    private sealed class ApplicationModelProviderTypeInjectionFactory(IServiceCollection services) : IHttpMessageTypesInjector<IServiceCollection>
    {
        public IServiceCollection WithInjectedTypes<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            TMessage,
            TResponse>()
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, HttpMessageEndpointApplicationModelProvider<TMessage, TResponse>>());

            return services;
        }
    }
}
