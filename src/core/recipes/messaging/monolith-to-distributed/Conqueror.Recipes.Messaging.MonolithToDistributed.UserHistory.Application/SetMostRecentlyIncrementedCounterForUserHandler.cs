namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;

internal partial class SetMostRecentlyIncrementedCounterForUserHandler(IUserHistoryWriteRepository repository)
    : SetMostRecentlyIncrementedCounterForUser.IHandler
{
    public static void ConfigurePipeline(SetMostRecentlyIncrementedCounterForUser.IPipeline pipeline) => pipeline.UseDefault();

    public async Task Handle(SetMostRecentlyIncrementedCounterForUser message, CancellationToken cancellationToken = default)
    {
        await repository.SetMostRecentlyIncrementedCounter(message.UserId, message.CounterName);
    }
}
