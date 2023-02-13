namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler
{
    private readonly CountersRepository repository;

    public GetCounterValueQueryHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
