namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

public sealed record DeleteCounterCommand(string CounterName);

// a command handler does not need to have a response
public interface IDeleteCounterCommandHandler : ICommandHandler<DeleteCounterCommand>
{
}

internal sealed class DeleteCounterCommandHandler : IDeleteCounterCommandHandler
{
    private readonly CountersRepository repository;

    public DeleteCounterCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task ExecuteCommand(DeleteCounterCommand command, CancellationToken cancellationToken = default)
    {
        await repository.DeleteCounter(command.CounterName);
    }
}
