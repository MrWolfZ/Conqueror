namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy);

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public interface IIncrementCounterByCommandHandler : ICommandHandler<IncrementCounterByCommand, IncrementCounterByCommandResponse>
{
}

internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler
{
    private readonly CountersRepository repository;

    public IncrementCounterByCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(command.CounterName);
        await repository.SetCounterValue(command.CounterName, counterValue + command.IncrementBy);
        return new(counterValue + command.IncrementBy);
    }
}
