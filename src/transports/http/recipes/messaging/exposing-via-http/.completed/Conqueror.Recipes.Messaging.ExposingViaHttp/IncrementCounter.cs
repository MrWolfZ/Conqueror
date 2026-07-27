namespace Conqueror.Recipes.Messaging.ExposingViaHttp;

[HttpMessage<IncrementCounterResponse>(Version = "v1", ApiGroupName = "Counters")]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(CountersRepository repository) : IncrementCounter.IHandler
{
    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await repository.SetCounterValue(message.CounterName, newCounterValue);
        return new IncrementCounterResponse(newCounterValue);
    }
}
