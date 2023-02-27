namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture;

[HttpQuery]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler, IConfigureQueryPipeline
{
    private readonly CountersRepository repository;

    public GetCounterValueQueryHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.UseDefault();

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
