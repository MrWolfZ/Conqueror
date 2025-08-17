namespace Quickstart.Enhanced;

using Conqueror;

internal sealed partial class IncrementCounterByAmountHandler(
    CountersRepository repository,
    ISignalPublishers publishers
) : IncrementCounterByAmount.IHandler
{
    public static void ConfigurePipeline(IncrementCounterByAmount.IPipeline pipeline) =>
        pipeline.UseDefault();

    public async Task<CounterIncrementedResponse> Handle(
        IncrementCounterByAmount message,
        CancellationToken cancellationToken = default
    )
    {
        var newValue = await repository.AddOrIncrementCounter(
            message.CounterName,
            message.IncrementBy
        );

        await publishers
            .For(CounterIncremented.T)
            .WithDefaultPublisherPipeline(typeof(IncrementCounterByAmountHandler))
            .WithInProcessAndServerSentEventsTransport()
            .Handle(new(message.CounterName, newValue, message.IncrementBy), cancellationToken);

        return new CounterIncrementedResponse(
            await repository.GetCounterValue(message.CounterName)
        );
    }
}
