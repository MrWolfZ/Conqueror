using Conqueror.Eventing;
using Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

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
    [ValidateCommand]
    public async Task<IncrementSharedCounterValueCommandResponse> ExecuteCommand(IncrementSharedCounterValueCommand command, CancellationToken cancellationToken)
    {
        var valueAfterIncrement = counter.IncrementBy(command.IncrementBy);
        await eventObserver.HandleEvent(new(valueAfterIncrement, command.IncrementBy), cancellationToken);
        return new(valueAfterIncrement);
    }
}
