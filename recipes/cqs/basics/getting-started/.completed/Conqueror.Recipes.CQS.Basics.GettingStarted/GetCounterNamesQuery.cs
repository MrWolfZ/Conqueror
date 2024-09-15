namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record GetCounterNamesQuery;

public sealed record GetCounterNamesQueryResponse(IReadOnlyCollection<string> CounterNames);

internal sealed class GetCounterNamesQueryHandler(CountersRepository repository) : IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>
{
    public async Task<GetCounterNamesQueryResponse> Handle(GetCounterNamesQuery query, CancellationToken cancellationToken = default)
    {
        var counters = await repository.GetCounters();
        return new(counters.Keys.ToList());
    }
}
