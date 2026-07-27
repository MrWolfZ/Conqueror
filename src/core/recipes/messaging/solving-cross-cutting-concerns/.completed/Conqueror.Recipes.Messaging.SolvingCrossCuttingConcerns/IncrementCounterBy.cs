namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

[Message<IncrementCounterByResponse>]
public partial record IncrementCounterBy(string CounterName, int IncrementBy)
{
    [Range(1, int.MaxValue, ErrorMessage = "invalid amount to increment by, it must be a strictly positive integer")]
    public int IncrementBy { get; } = IncrementBy;
}

public record IncrementCounterByResponse(int NewCounterValue);

internal partial class IncrementCounterByHandler(CountersRepository repository) : IncrementCounterBy.IHandler
{
    public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
        pipeline.UseDefault()
                .ConfigureRetry(o => o.RetryAttemptLimit = 3);

    public async Task<IncrementCounterByResponse> Handle(IncrementCounterBy message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        await repository.SetCounterValue(message.CounterName, counterValue + message.IncrementBy);
        return new IncrementCounterByResponse(counterValue + message.IncrementBy);
    }
}
