namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;

internal sealed class SetMostRecentlyIncrementedCounterForUserCommandHandler(
    IUserHistoryWriteRepository userHistoryWriteRepository)
    : ISetMostRecentlyIncrementedCounterForUserCommandHandler
{
    public static void ConfigurePipeline(ICommandPipeline<SetMostRecentlyIncrementedCounterForUserCommand> pipeline) => pipeline.UseDefault();

    public async Task ExecuteCommand(SetMostRecentlyIncrementedCounterForUserCommand command, CancellationToken cancellationToken = default)
    {
        await userHistoryWriteRepository.SetMostRecentlyIncrementedCounter(command.UserId, command.CounterName);
    }
}
