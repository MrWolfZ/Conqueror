namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounter;

internal sealed class DecrementSharedCounterValueCommandHandler : IDecrementSharedCounterValueCommandHandler
{
    private readonly SharedCounter counter;

    public DecrementSharedCounterValueCommandHandler(SharedCounter counter)
    {
        this.counter = counter;
    }

    [GatherCommandMetrics]
    [LogCommand]
    [RequiresCommandPermission(Permissions.UseSharedCounter)]
    [ValidateCommand]
    [ExecuteInTransaction(EnlistInAmbientTransaction = false)]
    [RetryCommand(MaxNumberOfAttempts = 2, RetryIntervalInSeconds = 2)]
    [CommandTimeout]
    public async Task<DecrementSharedCounterValueCommandResponse> ExecuteCommand(
        DecrementSharedCounterValueCommand command, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var valueAfterDecrement = counter.DecrementBy(command.DecrementBy);
        return new(valueAfterDecrement);
    }
}