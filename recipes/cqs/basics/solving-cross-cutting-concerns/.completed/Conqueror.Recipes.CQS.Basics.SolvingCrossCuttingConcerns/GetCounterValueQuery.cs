namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>;

internal sealed class GetCounterValueQueryHandler(CountersRepository repository) : IGetCounterValueQueryHandler
{
    public static void ConfigurePipeline(IQueryPipeline<GetCounterValueQuery, GetCounterValueQueryResponse> pipeline) =>
        pipeline.UseDefault()
                .ConfigureRetry(o => o.RetryAttemptLimit = 3);

    public async Task<GetCounterValueQueryResponse> Handle(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        return new(await repository.GetCounterValue(query.CounterName));
    }
}
