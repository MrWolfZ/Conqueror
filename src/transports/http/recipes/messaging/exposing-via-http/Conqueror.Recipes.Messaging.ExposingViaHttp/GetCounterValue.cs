namespace Conqueror.Recipes.Messaging.ExposingViaHttp;

[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);

internal partial class GetCounterValueHandler(CountersRepository repository) : GetCounterValue.IHandler
{
    public async Task<GetCounterValueResponse> Handle(GetCounterValue message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        return new GetCounterValueResponse(counterValue.HasValue, counterValue);
    }
}
