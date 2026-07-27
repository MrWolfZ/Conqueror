namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;

internal partial class IncrementCounterHandler(
    ICountersReadRepository countersReadRepository,
    ICountersWriteRepository countersWriteRepository,
    IMessageSenders senders)
    : IncrementCounter.IHandler
{
    public static void ConfigurePipeline(IncrementCounter.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersReadRepository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersWriteRepository.SetCounterValue(message.CounterName, newCounterValue);

        // integrate with the UserHistory context through its contract, without a direct dependency on its implementation
        await senders.For(SetMostRecentlyIncrementedCounterForUser.T)
                     .Handle(new SetMostRecentlyIncrementedCounterForUser(message.UserId, message.CounterName), cancellationToken);

        return new IncrementCounterResponse(newCounterValue);
    }
}
