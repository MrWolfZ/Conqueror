namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

[HttpMessage<IncrementCounterResponse>(HttpMethod = "POST")]
public partial record IncrementCounter(string CounterName, string UserId);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(
    ICountersReadRepository countersReadRepository,
    ICountersWriteRepository countersWriteRepository,
    IUserHistoryWriteRepository userHistoryWriteRepository)
    : IncrementCounter.IHandler
{
    public static void ConfigurePipeline(IncrementCounter.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersReadRepository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersWriteRepository.SetCounterValue(message.CounterName, newCounterValue);
        await userHistoryWriteRepository.SetMostRecentlyIncrementedCounter(message.UserId, message.CounterName);
        return new IncrementCounterResponse(newCounterValue);
    }
}
