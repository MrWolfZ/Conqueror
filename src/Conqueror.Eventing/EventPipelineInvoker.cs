using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal static class EventPipelineInvoker
{
    public static Task RunPipeline<TEvent, TObservedEvent>(
        TEvent evt,
        Action<IEventPipeline<TObservedEvent>>? configurePipeline,
        EventTransportAttribute attribute,
        IServiceProvider serviceProvider,
        EventTransportRole transportRole,
        Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
        CancellationToken cancellationToken)
        where TEvent : class, TObservedEvent
        where TObservedEvent : class
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var transportTypeName = attribute.TransportTypeName;
        var transportType = new EventTransportType(transportTypeName, transportRole);

        // TODO: add context support
        // if (conquerorContext.GetEventId() is null || isInProcessClient)
        // {
        //     conquerorContext.SetEventId(ActivitySpanId.CreateRandom().ToString());
        // }

        var pipelineBuilder = new EventPipeline<TEvent, TObservedEvent>(serviceProvider, conquerorContext, transportType);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return pipeline.Execute(serviceProvider, observerFn, evt, transportType, cancellationToken);
    }
}
