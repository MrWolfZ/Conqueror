using Conqueror;

namespace Quickstart.Enhanced;

internal sealed partial class DoublingCounterIncrementedHandler(
    IMessageSenders senders)
    : CounterIncremented.IHandler
{
    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
        pipeline.SkipSignalMatching<T, CounterIncremented>(s => s.CounterName != "doubler")
                .EnsureSingleExecutionPerOperation()
                .UseLoggingWithIndentedJson();

    public async Task Handle(
        CounterIncremented signal,
        CancellationToken cancellationToken = default)
    {
        await senders.For(IncrementCounterByAmount.T)
                     .WithDefaultSenderPipeline()
                     .Handle(new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                             cancellationToken);
    }
}
