namespace Conqueror.Recipes.Signalling.GettingStarted;

internal partial class NotificationHandler : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        Console.WriteLine($"notification: counter '{signal.CounterName}' was incremented to {signal.NewValue}");
    }
}
