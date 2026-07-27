namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(int CounterValue);

internal partial class GetCounterValueHandler(CountersRepository repository) : GetCounterValue.IHandler
{
    public static void ConfigurePipeline(GetCounterValue.IPipeline pipeline) =>
        pipeline.UseDefault()
                .ConfigureRetry(o => o.RetryAttemptLimit = 3);

    public async Task<GetCounterValueResponse> Handle(GetCounterValue message, CancellationToken cancellationToken = default)
    {
        return new GetCounterValueResponse(await repository.GetCounterValue(message.CounterName));
    }
}
