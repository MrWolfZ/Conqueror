namespace Conqueror.Recipes.Messaging.TestingHttp;

[HttpMessage<IncrementCounterResponse>(Version = "v1")]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(CountersRepository repository) : IncrementCounter.IHandler
{
    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);

        if (counterValue >= 1000)
        {
            throw new CounterValueLimitReachedException(message.CounterName);
        }

        var newCounterValue = (counterValue ?? 0) + 1;
        await repository.SetCounterValue(message.CounterName, newCounterValue);
        return new IncrementCounterResponse(newCounterValue);
    }
}

public class CounterValueLimitReachedException(string counterName) : Exception
{
    public string CounterName { get; } = counterName;
}
