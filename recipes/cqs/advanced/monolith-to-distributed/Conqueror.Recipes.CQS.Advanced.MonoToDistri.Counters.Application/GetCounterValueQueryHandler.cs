namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler
{
    private readonly ICountersReadRepository repository;

    public GetCounterValueQueryHandler(ICountersReadRepository repository)
    {
        this.repository = repository;
    }

    public static void ConfigurePipeline(IQueryPipeline<GetCounterValueQuery, GetCounterValueQueryResponse> pipeline) => pipeline.UseDefault();

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
