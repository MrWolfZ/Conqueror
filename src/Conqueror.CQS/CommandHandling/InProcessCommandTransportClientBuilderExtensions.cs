using System;
using Conqueror.CQS.CommandHandling;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessCommandTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseInProcess(this ICommandTransportClientBuilder builder)
    {
        var registration = builder.ServiceProvider
                                  .GetRequiredService<CommandTransportRegistry>()
                                  .GetCommandHandlerRegistration(builder.CommandType);

        if (registration is null)
        {
            throw new InvalidOperationException($"there is no handler registered for command type '{builder.CommandType}'");
        }

        return new InProcessCommandTransport(registration.HandlerType, registration.ConfigurePipeline);
    }
}
