namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message<GetCounterNamesResponse>]
public partial record GetCounterNames;

public record GetCounterNamesResponse(IReadOnlyCollection<string> CounterNames);

internal partial class GetCounterNamesHandler(CountersRepository repository) : GetCounterNames.IHandler
{
    public async Task<GetCounterNamesResponse> Handle(
        GetCounterNames message,
        CancellationToken cancellationToken = default
    )
    {
        var counters = await repository.GetCounters();
        return new GetCounterNamesResponse(counters.Keys.ToList());
    }
}
