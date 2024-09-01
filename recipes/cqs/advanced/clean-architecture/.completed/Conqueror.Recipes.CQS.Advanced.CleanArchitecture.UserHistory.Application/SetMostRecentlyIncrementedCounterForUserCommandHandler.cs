namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;

internal sealed class SetMostRecentlyIncrementedCounterForUserCommandHandler : ISetMostRecentlyIncrementedCounterForUserCommandHandler
{
    private readonly IUserHistoryWriteRepository userHistoryWriteRepository;

    public SetMostRecentlyIncrementedCounterForUserCommandHandler(IUserHistoryWriteRepository userHistoryWriteRepository)
    {
        this.userHistoryWriteRepository = userHistoryWriteRepository;
    }

    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline.UseDefault();

    public async Task ExecuteCommand(SetMostRecentlyIncrementedCounterForUserCommand command, CancellationToken cancellationToken = default)
    {
        await userHistoryWriteRepository.SetMostRecentlyIncrementedCounter(command.UserId, command.CounterName);
    }
}
