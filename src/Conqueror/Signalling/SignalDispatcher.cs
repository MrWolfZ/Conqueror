using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalDispatcher(
    IConquerorContextAccessor conquerorContextAccessor,
    ISignalIdFactory signalIdFactory,
    SignalTransportRole transportRole,
    Type? handlerType)
    : ISignalDispatcher
{
    public async Task Dispatch<TSignal>(
        TSignal signal,
        IServiceProvider serviceProvider,
        Action<ISignalPipeline<TSignal>>? configurePipeline,
        ISignalPublisher<TSignal>? publisher,
        ConfigureSignalPublisher<TSignal>? configurePublisher,
        ConfigureSignalPublisherAsync<TSignal>? configurePublisherAsync,
        CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();

        var originalSignalId = conquerorContext.GetSignalId();

        // ensure that a signal ID is available for the transport client factory
        if (originalSignalId is null)
        {
            conquerorContext.SetSignalId(signalIdFactory.GenerateId());
        }

        // if we are in a publish operation, make sure to create a new signal ID for this execution if
        // we were called from within the call context of another handler
        if (originalSignalId is not null && transportRole is SignalTransportRole.Publisher)
        {
            conquerorContext.SetSignalId(signalIdFactory.GenerateId());
        }

        if (publisher is null)
        {
            var transportBuilder = new SignalPublisherBuilder<TSignal>(serviceProvider, conquerorContext);

            if (configurePublisher is not null)
            {
                publisher = configurePublisher(transportBuilder);
            }
            else if (configurePublisherAsync is not null)
            {
                publisher = await configurePublisherAsync(transportBuilder).ConfigureAwait(false);
            }
            else
            {
                publisher = transportBuilder.UseInProcessWithSequentialBroadcastingStrategy();
            }
        }

        var transportType = new SignalTransportType(publisher.TransportTypeName, transportRole);

        var initialCapacity = transportRole is SignalTransportRole.Publisher
            ? PipelineCapacityCache<TSignal>.MaxObservedPublisherPipelineCapacity
            : PipelineCapacityCache<TSignal>.MaxObservedHandlerPipelineCapacity;

        var pipeline = new SignalPipeline<TSignal>(
            handlerType,
            serviceProvider,
            conquerorContext,
            transportType,
            initialCapacity);

        configurePipeline?.Invoke(pipeline);

        if (pipeline.Count > initialCapacity)
        {
            if (transportRole is SignalTransportRole.Publisher)
            {
                PipelineCapacityCache<TSignal>.MaxObservedPublisherPipelineCapacity = pipeline.Count;
            }
            else
            {
                PipelineCapacityCache<TSignal>.MaxObservedHandlerPipelineCapacity = pipeline.Count;
            }
        }

        await pipeline.Execute(
                          serviceProvider,
                          signal,
                          publisher,
                          transportType,
                          cancellationToken)
                      .ConfigureAwait(false);
    }
}

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "intentional design to leverage static classes as cache")]
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "used as static lookup key")]
file static class PipelineCapacityCache<TSignal>
{
    public static int MaxObservedPublisherPipelineCapacity { get; set; }

    public static int MaxObservedHandlerPipelineCapacity { get; set; }
}
