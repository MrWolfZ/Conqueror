namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Application;

internal sealed class GetCounterValueQueryHandler(ICountersReadRepository repository) : IGetCounterValueQueryHandler
{
    public static void ConfigurePipeline(IQueryPipeline<GetCounterValueQuery, GetCounterValueQueryResponse> pipeline) => pipeline.UseDefault();

    public async Task<GetCounterValueQueryResponse> Handle(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
