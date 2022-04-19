using Conqueror.Eventing;

namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounter;

internal sealed class IncrementSharedCounterValueCommandHandler : IIncrementSharedCounterValueCommandHandler
{
    private readonly SharedCounter counter;
    private readonly IEventObserver<SharedCounterIncrementedEvent> eventObserver;

    public IncrementSharedCounterValueCommandHandler(SharedCounter counter, IEventObserver<SharedCounterIncrementedEvent> eventObserver)
    {
        this.counter = counter;
        this.eventObserver = eventObserver;
    }

    [LogCommand]
    [RequiresCommandPermission(Permissions.UseSharedCounter)]
    [ValidateCommand]
    [ExecuteInTransaction(EnlistInAmbientTransaction = false)]
    [RetryCommand(MaxNumberOfAttempts = 2, RetryIntervalInSeconds = 2)]
    [CommandTimeout]
    [GatherCommandMetrics]
    public async Task<IncrementSharedCounterValueCommandResponse> ExecuteCommand(IncrementSharedCounterValueCommand command, CancellationToken cancellationToken)
    {
        var valueAfterIncrement = counter.IncrementBy(command.IncrementBy);
        await eventObserver.HandleEvent(new(valueAfterIncrement, command.IncrementBy), cancellationToken);
        return new(valueAfterIncrement);
    }
}
