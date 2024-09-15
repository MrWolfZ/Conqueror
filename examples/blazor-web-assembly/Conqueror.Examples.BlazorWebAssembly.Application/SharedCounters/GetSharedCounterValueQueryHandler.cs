using Conqueror.Examples.BlazorWebAssembly.Domain;
using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application.SharedCounters;

internal sealed class GetSharedCounterValueQueryHandler(SharedCounter counter) : IGetSharedCounterValueQueryHandler
{
    public async Task<GetSharedCounterValueQueryResponse> ExecuteQuery(GetSharedCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return new(counter.GetValue());
    }

    // ReSharper disable once UnusedMember.Global
    public static void ConfigurePipeline(IQueryPipeline<GetSharedCounterValueQuery, GetSharedCounterValueQueryResponse> pipeline) =>
        pipeline.UseDefault()
                .UseCaching(TimeSpan.FromMinutes(1), invalidateResultsOnEventTypes: [typeof(SharedCounterIncrementedEvent)])
                .ConfigureTimeout(TimeSpan.FromSeconds(10))
                .RequirePermission(Permissions.UseSharedCounter);
}
