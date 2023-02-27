namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

[HttpQuery]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler, IConfigureQueryPipeline
{
    private readonly ICountersReadRepository repository;

    public GetCounterValueQueryHandler(ICountersReadRepository repository)
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
