namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy)
{
    [Range(1, int.MaxValue, ErrorMessage = "invalid amount to increment by, it must be a strictly positive integer")]
    public int IncrementBy { get; } = IncrementBy;
}

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public interface IIncrementCounterByCommandHandler : ICommandHandler<IncrementCounterByCommand, IncrementCounterByCommandResponse>;

internal sealed class IncrementCounterByCommandHandler(CountersRepository repository) : IIncrementCounterByCommandHandler
{
    public static void ConfigurePipeline(ICommandPipeline<IncrementCounterByCommand, IncrementCounterByCommandResponse> pipeline) =>
        pipeline.UseDefault()
                .ConfigureRetry(o => o.RetryAttemptLimit = 3);

    public async Task<IncrementCounterByCommandResponse> Handle(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(command.CounterName);
        await repository.SetCounterValue(command.CounterName, counterValue + command.IncrementBy);
        return new(counterValue + command.IncrementBy);
    }
}
