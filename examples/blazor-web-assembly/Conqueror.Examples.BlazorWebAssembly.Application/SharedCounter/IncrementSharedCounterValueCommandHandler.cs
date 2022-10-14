namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounter;

internal sealed class IncrementSharedCounterValueCommandHandler : IIncrementSharedCounterValueCommandHandler, IConfigureCommandPipeline
{
    private readonly SharedCounter counter;
    private readonly ISharedCounterIncrementedEventObserver eventObserver;

    public IncrementSharedCounterValueCommandHandler(SharedCounter counter, ISharedCounterIncrementedEventObserver eventObserver)
    {
        this.counter = counter;
        this.eventObserver = eventObserver;
    }

    public async Task<IncrementSharedCounterValueCommandResponse> ExecuteCommand(IncrementSharedCounterValueCommand command, CancellationToken cancellationToken)
    {
        var valueAfterIncrement = counter.IncrementBy(command.IncrementBy);
        await eventObserver.HandleEvent(new(valueAfterIncrement, command.IncrementBy), cancellationToken);
        return new(valueAfterIncrement);
    }

    // ReSharper disable once UnusedMember.Global
    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
        pipeline.UseDefault()
                .ConfigureTimeout(TimeSpan.FromSeconds(10))
                .RequirePermission(Permissions.UseSharedCounter)
                .ConfigureRetry(2, TimeSpan.FromSeconds(2))
                .OutsideOfAmbientTransaction();
}
