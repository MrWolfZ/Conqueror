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

    // ReSharper disable once UnusedMember.Global
    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) =>
        pipeline.UseMetrics()
                .UseLogging()
                .UsePermission(Permissions.UseSharedCounter)
                .UseValidation()
                .UseCaching(TimeSpan.FromMinutes(1), invalidateResultsOnEventTypes: new[] { typeof(SharedCounterIncrementedEvent) })
                .UseTimeout(TimeSpan.FromSeconds(10));
}
