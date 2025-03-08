using System;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessQueryTransportClientBuilderExtensions
{
    public static IQueryTransportClient UseInProcess(this IQueryTransportClientBuilder builder)
    {
        var registration = builder.ServiceProvider
                                  .GetRequiredService<QueryTransportRegistry>()
                                  .GetQueryHandlerRegistration(builder.QueryType);

        if (registration is null)
        {
            throw new InvalidOperationException($"there is no handler registered for query type '{builder.QueryType}'");
        }

        return new InProcessQueryTransport(registration.HandlerType, registration.ConfigurePipeline);
    }
}
