namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounter;

internal sealed class GetSharedCounterValueQueryHandler : IGetSharedCounterValueQueryHandler
{
    private readonly SharedCounter counter;

    public GetSharedCounterValueQueryHandler(SharedCounter counter)
    {
        this.counter = counter;
    }

    [LogQuery]
    [ValidateQuery]
    [CacheQueryResult(invalidateResultsAfterSeconds: 1, InvalidateResultsOnEventTypes = new []{ typeof(SharedCounterIncrementedEvent) })]
    public async Task<GetSharedCounterValueQueryResponse> ExecuteQuery(GetSharedCounterValueQuery query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new(counter.GetValue());
    }
}
