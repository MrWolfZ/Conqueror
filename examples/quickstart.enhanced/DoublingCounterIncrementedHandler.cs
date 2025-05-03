using Conqueror;

namespace Quickstart.Enhanced;

internal sealed partial class DoublingCounterIncrementedHandler(
    IMessageSenders senders)
    : CounterIncremented.IHandler
{
    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
        pipeline.SkipSignalMatching((CounterIncremented s) => s.CounterName != "doubler")
                .EnsureSingleExecutionPerOperation(nameof(DoublingCounterIncrementedHandler))
                .UseDefault();

    public async Task Handle(
        CounterIncremented signal,
        CancellationToken cancellationToken = default)
    {
        await senders.For(IncrementCounterByAmount.T)
                     .WithDefaultSenderPipeline(typeof(DoublingCounterIncrementedHandler))
                     .Handle(new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                             cancellationToken);
    }
}
