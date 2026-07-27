namespace Conqueror.Recipes.Messaging.CleanArchitecture;

[HttpMessage<IncrementCounterResponse>(HttpMethod = "POST")]
public partial record IncrementCounter(string CounterName, string UserId);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(
    CountersRepository countersRepository,
    UserHistoryRepository userHistoryRepository)
    : IncrementCounter.IHandler
{
    public static void ConfigurePipeline(IncrementCounter.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersRepository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersRepository.SetCounterValue(message.CounterName, newCounterValue);
        await userHistoryRepository.SetMostRecentlyIncrementedCounter(message.UserId, message.CounterName);
        return new IncrementCounterResponse(newCounterValue);
    }
}
