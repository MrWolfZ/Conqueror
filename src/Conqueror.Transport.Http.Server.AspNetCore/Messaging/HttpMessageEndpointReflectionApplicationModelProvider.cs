using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

[RequiresDynamicCode("Needs to create generic types at runtime")]
internal sealed class HttpMessageEndpointReflectionApplicationModelProvider(
    IServiceProvider serviceProvider,
    ILogger<HttpMessageEndpointReflectionApplicationModelProvider> logger,
    IEnumerable<HttpMessageControllerRegistration> explicitControllerRegistrations)
    : IApplicationModelProvider
{
    /// <summary>
    ///     The <see cref="Microsoft.AspNetCore.Mvc.ApiControllerAttribute" /> attribute
    ///     model provider uses <c>-1000 + 100</c> - this needs to be applied first so
    ///     that we get attribute routing.<br />
    ///     <br />
    ///     The <see cref="HttpMessageEndpointApplicationModelProvider{TMessage,TResponse}" />
    ///     uses <c>-1000 + 50</c>. We want this to run after any specific controller providers.
    /// </summary>
    public int Order => -1000 + 51;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        var explicitlyConfiguredControllerTypes = explicitControllerRegistrations.Select(r => r.ControllerType)
                                                                                 .ToHashSet();

        logger.LogDebug("configuring message controllers for message types");

        foreach (var (messageType, responseType) in
                 from controller in context.Result.Controllers
                 where (controller.ControllerType.BaseType?.IsGenericType ?? false)
                       && controller.ControllerType.BaseType.GetGenericTypeDefinition() == typeof(MessageApiControllerBase<,>)
                 where !explicitlyConfiguredControllerTypes.Contains(controller.ControllerType)
                 let messageType = controller.ControllerType.GetGenericArguments()[0]
                 let responseType = controller.ControllerType.GetGenericArguments().ElementAtOrDefault(1) ?? typeof(UnitMessageResponse)
                 select (messageType, responseType))
        {
            var modelProviderType = typeof(HttpMessageEndpointApplicationModelProvider<,>).MakeGenericType(messageType, responseType);
            var modelProvider = (IApplicationModelProvider)serviceProvider.GetRequiredService(modelProviderType);
            modelProvider.OnProvidersExecuting(context);
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // nothing to dd
    }
}
