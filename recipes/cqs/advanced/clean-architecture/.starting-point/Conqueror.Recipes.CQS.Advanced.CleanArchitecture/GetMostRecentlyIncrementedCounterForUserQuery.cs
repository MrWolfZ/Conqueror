namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture;

[HttpQuery]
public sealed record GetMostRecentlyIncrementedCounterForUserQuery([Required] string UserId);

public sealed record GetMostRecentlyIncrementedCounterForUserQueryResponse(string? CounterName);

public interface IGetMostRecentlyIncrementedCounterForUserQueryHandler : IQueryHandler<GetMostRecentlyIncrementedCounterForUserQuery, GetMostRecentlyIncrementedCounterForUserQueryResponse>
{
}

internal sealed class GetMostRecentlyIncrementedCounterForUserQueryHandler : IGetMostRecentlyIncrementedCounterForUserQueryHandler, IConfigureQueryPipeline
{
    private readonly UserHistoryRepository repository;

    public GetMostRecentlyIncrementedCounterForUserQueryHandler(UserHistoryRepository repository)
    {
        this.repository = repository;
    }

    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.UseDefault();

    public async Task<GetMostRecentlyIncrementedCounterForUserQueryResponse> ExecuteQuery(GetMostRecentlyIncrementedCounterForUserQuery query, CancellationToken cancellationToken = default)
    {
        var counterName = await repository.GetMostRecentlyIncrementedCounterByUserId(query.UserId);
        return new(counterName);
    }
}
