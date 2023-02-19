namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture;

[HttpCommand]
public sealed record IncrementCounterCommand([Required] string CounterName, [Required] string UserId);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>
{
}

internal sealed class IncrementCounterCommandHandler : IIncrementCounterCommandHandler
{
    private readonly CountersRepository countersRepository;
    private readonly UserHistoryRepository userHistoryRepository;

    public IncrementCounterCommandHandler(CountersRepository countersRepository, UserHistoryRepository userHistoryRepository)
    {
        this.countersRepository = countersRepository;
        this.userHistoryRepository = userHistoryRepository;
    }

    public async Task<IncrementCounterCommandResponse> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersRepository.GetCounterValue(command.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersRepository.SetCounterValue(command.CounterName, newCounterValue);
        await userHistoryRepository.SetMostRecentlyIncrementedCounter(command.UserId, command.CounterName);
        return new(newCounterValue);
    }
}
