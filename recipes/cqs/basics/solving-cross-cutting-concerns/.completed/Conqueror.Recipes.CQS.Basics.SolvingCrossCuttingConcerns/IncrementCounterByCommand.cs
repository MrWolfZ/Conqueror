namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy)
{
    [Range(1, int.MaxValue, ErrorMessage = "invalid amount to increment by, it must be a strictly positive integer")]
    public int IncrementBy { get; } = IncrementBy;
}

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public interface IIncrementCounterByCommandHandler : ICommandHandler<IncrementCounterByCommand, IncrementCounterByCommandResponse>
{
}

internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler, IConfigureCommandPipeline
{
    private readonly CountersRepository repository;

    public IncrementCounterByCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
        pipeline.UseDefault()
                .ConfigureRetry(o => o.RetryAttemptLimit = 3);

    public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(command.CounterName);
        await repository.SetCounterValue(command.CounterName, counterValue + command.IncrementBy);
        return new(counterValue + command.IncrementBy);
    }
}
