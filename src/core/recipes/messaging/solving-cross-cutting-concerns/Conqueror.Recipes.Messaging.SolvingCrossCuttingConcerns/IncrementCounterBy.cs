namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

[Message<IncrementCounterByResponse>]
public partial record IncrementCounterBy(string CounterName, int IncrementBy);

public record IncrementCounterByResponse(int NewCounterValue);

internal partial class IncrementCounterByHandler(CountersRepository repository) : IncrementCounterBy.IHandler
{
    public async Task<IncrementCounterByResponse> Handle(IncrementCounterBy message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        await repository.SetCounterValue(message.CounterName, counterValue + message.IncrementBy);
        return new IncrementCounterByResponse(counterValue + message.IncrementBy);
    }
}
