namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message]
public partial record DeleteCounter(string CounterName);

internal partial class DeleteCounterHandler(CountersRepository repository) : DeleteCounter.IHandler
{
    public async Task Handle(DeleteCounter message, CancellationToken cancellationToken = default)
    {
        await repository.DeleteCounter(message.CounterName);
    }
}
