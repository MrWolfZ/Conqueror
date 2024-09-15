namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record DeleteCounterCommand(string CounterName);

// a command handler does not need to have a response
public interface IDeleteCounterCommandHandler : ICommandHandler<DeleteCounterCommand>;

internal sealed class DeleteCounterCommandHandler(CountersRepository repository) : IDeleteCounterCommandHandler
{
    public async Task Handle(DeleteCounterCommand command, CancellationToken cancellationToken = default)
    {
        await repository.DeleteCounter(command.CounterName);
    }
}
