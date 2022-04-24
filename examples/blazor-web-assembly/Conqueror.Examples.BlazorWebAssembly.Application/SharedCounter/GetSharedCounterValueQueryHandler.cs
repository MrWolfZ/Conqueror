namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounter;

internal sealed class GetSharedCounterValueQueryHandler : IGetSharedCounterValueQueryHandler
{
    private readonly SharedCounter counter;

    public GetSharedCounterValueQueryHandler(SharedCounter counter)
    {
        this.counter = counter;
    }

    public async Task<GetSharedCounterValueQueryResponse> ExecuteQuery(GetSharedCounterValueQuery query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new(counter.GetValue());
    }

    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) =>
        pipeline.UseDefault()
                .UseCaching(TimeSpan.FromMinutes(1), invalidateResultsOnEventTypes: new[] { typeof(SharedCounterIncrementedEvent) })
                .ConfigureTimeout(TimeSpan.FromSeconds(10))
                .RequirePermission(Permissions.UseSharedCounter);
}
