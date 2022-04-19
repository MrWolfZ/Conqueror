namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounter;

internal sealed class GetSharedCounterValueQueryHandler : IGetSharedCounterValueQueryHandler
{
    private readonly SharedCounter counter;

    public GetSharedCounterValueQueryHandler(SharedCounter counter)
    {
        this.counter = counter;
    }

    [LogQuery]
    [RequiresQueryPermission(Permissions.UseSharedCounter)]
    [ValidateQuery]
    [CacheQueryResult(invalidateResultsAfterSeconds: 60, InvalidateResultsOnEventTypes = new []{ typeof(SharedCounterIncrementedEvent) })]
    [QueryTimeout(TimeoutAfterSeconds = 10)]
    [GatherQueryMetrics]
    public async Task<GetSharedCounterValueQueryResponse> ExecuteQuery(GetSharedCounterValueQuery query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new(counter.GetValue());
    }
}
