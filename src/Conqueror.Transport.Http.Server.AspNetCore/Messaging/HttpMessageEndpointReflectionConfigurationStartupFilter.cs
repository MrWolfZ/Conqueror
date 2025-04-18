using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class HttpMessageEndpointReflectionConfigurationStartupFilter(
    ApplicationPartManager applicationPartManager,
    IMessageTransportRegistry messageTransportRegistry,
    HttpEndpointActionDescriptorChangeProvider actionDescriptorChangeProvider,
    Predicate<Type>? messageTypeFilter)
    : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return b =>
        {
            applicationPartManager.FeatureProviders.Add(new HttpMessageEndpointReflectionControllerFeatureProvider(messageTransportRegistry, messageTypeFilter));

            // in classic apps with a Startup.cs the startup filters run before the Startup.Configure method; but for a modern app
            // using top-level statements, the startup filter runs after the pipeline was configured (and controllers were detected),
            // and therefore we need to explicitly signal that controllers need to be detected again
            actionDescriptorChangeProvider.Signal();

            next(b);
        };
    }
}
