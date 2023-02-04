namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

public sealed record GetCounterNamesQuery;

public sealed record GetCounterNamesQueryResponse(IReadOnlyCollection<string> CounterNames);

internal sealed class GetCounterNamesQueryHandler : IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>
{
    private readonly CountersRepository repository;

    public GetCounterNamesQueryHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GetCounterNamesQueryResponse> ExecuteQuery(GetCounterNamesQuery query, CancellationToken cancellationToken = default)
    {
        var counters = await repository.GetCounters();
        return new(counters.Keys.ToList());
    }
}
