namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message<IncrementCounterResponse>]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(CountersRepository repository) : IncrementCounter.IHandler
{
    public async Task<IncrementCounterResponse> Handle(
        IncrementCounter message,
        CancellationToken cancellationToken = default
    )
    {
        var counterValue = await GetCounterValue(message.CounterName);
        await repository.SetCounterValue(message.CounterName, counterValue + 1);
        return new IncrementCounterResponse(counterValue + 1);
    }

    private async Task<int> GetCounterValue(string counterName)
    {
        try
        {
            return await repository.GetCounterValue(counterName);
        }
        catch (CounterNotFoundException)
        {
            return 0;
        }
    }
}
