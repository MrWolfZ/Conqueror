using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Conqueror.Transport.Http.Server.AspNetCore;

[RequiresDynamicCode("Needs to create generic types at runtime")]
internal sealed class HttpEndpointReflectionConfigurationStartupFilter(
    ApplicationPartManager applicationPartManager,
    IMessageTransportRegistry messageTransportRegistry,
    HttpEndpointActionDescriptorChangeProvider actionDescriptorChangeProvider)
    : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return b =>
        {
            applicationPartManager.FeatureProviders.Add(new HttpEndpointReflectionControllerFeatureProvider(messageTransportRegistry));

            // in classic apps with a Startup.cs the startup filters run before the Startup.Configure method; but for a modern app
            // using top-level statements, the startup filter runs after the pipeline was configured (and controllers were detected),
            // and therefore we need to explicitly signal that controllers need to be detected again
            actionDescriptorChangeProvider.Signal();

            next(b);
        };
    }
}
