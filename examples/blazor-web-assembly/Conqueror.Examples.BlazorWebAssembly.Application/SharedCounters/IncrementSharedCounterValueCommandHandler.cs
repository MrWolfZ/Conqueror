using Conqueror.Examples.BlazorWebAssembly.Domain;
using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounters;

internal sealed class IncrementSharedCounterValueCommandHandler(
    SharedCounter counter,
    ISharedCounterIncrementedEventObserver eventObserver)
    : IIncrementSharedCounterValueCommandHandler
{
    public async Task<IncrementSharedCounterValueCommandResponse> Handle(IncrementSharedCounterValueCommand command, CancellationToken cancellationToken = default)
    {
        var valueAfterIncrement = counter.IncrementBy(command.IncrementBy);
        await eventObserver.WithDefaultPublisherPipeline().Handle(new(valueAfterIncrement, command.IncrementBy), cancellationToken);
        return new(valueAfterIncrement);
    }

    // ReSharper disable once UnusedMember.Global
    public static void ConfigurePipeline(ICommandPipeline<IncrementSharedCounterValueCommand, IncrementSharedCounterValueCommandResponse> pipeline) =>
        pipeline.UseDefault()
                .ConfigureTimeout(TimeSpan.FromSeconds(10))
                .RequirePermission(Permissions.UseSharedCounter)
                .ConfigureRetry(2, TimeSpan.FromSeconds(2))
                .OutsideOfAmbientTransaction();
}
