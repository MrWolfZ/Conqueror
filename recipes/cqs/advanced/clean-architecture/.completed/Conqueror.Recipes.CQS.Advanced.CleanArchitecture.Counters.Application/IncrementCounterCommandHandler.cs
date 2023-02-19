using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Application;

internal sealed class IncrementCounterCommandHandler : IIncrementCounterCommandHandler
{
    private readonly ICountersReadRepository countersReadRepository;
    private readonly ICountersWriteRepository countersWriteRepository;
    private readonly ISetMostRecentlyIncrementedCounterForUserCommandHandler setMostRecentlyIncrementedCounterForUserCommandHandler;

    public IncrementCounterCommandHandler(ICountersReadRepository countersReadRepository,
                                          ICountersWriteRepository countersWriteRepository,
                                          ISetMostRecentlyIncrementedCounterForUserCommandHandler setMostRecentlyIncrementedCounterForUserCommandHandler)
    {
        this.countersReadRepository = countersReadRepository;
        this.countersWriteRepository = countersWriteRepository;
        this.setMostRecentlyIncrementedCounterForUserCommandHandler = setMostRecentlyIncrementedCounterForUserCommandHandler;
    }

    public async Task<IncrementCounterCommandResponse> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersReadRepository.GetCounterValue(command.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersWriteRepository.SetCounterValue(command.CounterName, newCounterValue);
        await setMostRecentlyIncrementedCounterForUserCommandHandler.ExecuteCommand(new(command.UserId, command.CounterName), cancellationToken);
        return new(newCounterValue);
    }
}
