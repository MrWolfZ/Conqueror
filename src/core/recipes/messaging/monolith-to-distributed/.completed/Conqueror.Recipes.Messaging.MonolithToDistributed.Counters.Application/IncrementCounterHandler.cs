namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;

internal partial class IncrementCounterHandler(
    ICountersReadRepository countersReadRepository,
    ICountersWriteRepository countersWriteRepository,
    SetMostRecentlyIncrementedCounterForUser.IHandler setMostRecentlyIncrementedCounterForUserHandler)
    : IncrementCounter.IHandler
{
    public static void ConfigurePipeline(IncrementCounter.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersReadRepository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersWriteRepository.SetCounterValue(message.CounterName, newCounterValue);

        // integrate with the UserHistory context through its contract; the transport over
        // which the message is sent is configured in the entry point's composition root
        await setMostRecentlyIncrementedCounterForUserHandler
            .Handle(new SetMostRecentlyIncrementedCounterForUser(message.UserId, message.CounterName), cancellationToken);

        return new IncrementCounterResponse(newCounterValue);
    }
}
