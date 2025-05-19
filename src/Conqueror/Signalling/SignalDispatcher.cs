using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Signalling;

internal sealed class SignalDispatcher<TSignal>(
    IServiceProvider serviceProvider,
    SignalPublisherFactory<TSignal> publisherFactory,
    Action<ISignalPipeline<TSignal>>? configurePipelineField,
    SignalTransportRole transportRole,
    Type? handlerType)
    : ISignalDispatcher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public async Task Dispatch(TSignal signal, CancellationToken cancellationToken)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();
        var signalIdFactory = serviceProvider.GetRequiredService<ISignalIdFactory>();

        var originalSignalId = conquerorContext.GetSignalId();

        // ensure that a signal ID is available for the transport client factory
        if (originalSignalId is null)
        {
            conquerorContext.SetSignalId(signalIdFactory.GenerateId());
        }

        var publisher = await publisherFactory.Create(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new SignalTransportType(publisher.TransportTypeName, transportRole);

        // if we are in a publish operation, make sure to create a new signal ID for this execution if
        // we were called from within the call context of another handler
        if (originalSignalId is not null && transportRole == SignalTransportRole.Publisher)
        {
            conquerorContext.SetSignalId(signalIdFactory.GenerateId());
        }

        var pipeline = new SignalPipeline<TSignal>(handlerType, serviceProvider, conquerorContext, transportType);

        configurePipelineField?.Invoke(pipeline);

        var pipelineRunner = pipeline.Build(conquerorContext);

        await pipelineRunner.Execute(serviceProvider,
                                     signal,
                                     publisher,
                                     transportType,
                                     cancellationToken)
                            .ConfigureAwait(false);
    }

    public ISignalDispatcher<TSignal> WithPipeline(Action<ISignalPipeline<TSignal>> configurePipeline)
        => new SignalDispatcher<TSignal>(
            serviceProvider,
            publisherFactory,
            pipeline =>
            {
                configurePipelineField?.Invoke(pipeline);
                configurePipeline(pipeline);
            },
            transportRole,
            handlerType);

    public ISignalDispatcher<TSignal> WithPublisher(ConfigureSignalPublisher<TSignal> configurePublisher)
        => new SignalDispatcher<TSignal>(
            serviceProvider,
            new(configurePublisher),
            configurePipelineField,
            transportRole,
            handlerType);

    public ISignalDispatcher<TSignal> WithPublisher(ConfigureSignalPublisherAsync<TSignal> configurePublisher)
        => new SignalDispatcher<TSignal>(
            serviceProvider,
            new(configurePublisher),
            configurePipelineField,
            transportRole,
            handlerType);
}
