namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler
{
    private readonly CountersRepository repository;

    public GetCounterValueQueryHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        return new(await repository.GetCounterValue(query.CounterName));
    }
}
