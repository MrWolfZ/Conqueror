namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(int CounterValue);

internal partial class GetCounterValueHandler(CountersRepository repository) : GetCounterValue.IHandler
{
    public async Task<GetCounterValueResponse> Handle(GetCounterValue message, CancellationToken cancellationToken = default)
    {
        return new GetCounterValueResponse(await repository.GetCounterValue(message.CounterName));
    }
}
