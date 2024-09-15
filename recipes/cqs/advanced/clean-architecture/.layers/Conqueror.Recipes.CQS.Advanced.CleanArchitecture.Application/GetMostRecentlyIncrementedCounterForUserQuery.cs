namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

[HttpQuery]
public sealed record GetMostRecentlyIncrementedCounterForUserQuery([Required] string UserId);

public sealed record GetMostRecentlyIncrementedCounterForUserQueryResponse(string? CounterName);

public interface IGetMostRecentlyIncrementedCounterForUserQueryHandler : IQueryHandler<GetMostRecentlyIncrementedCounterForUserQuery, GetMostRecentlyIncrementedCounterForUserQueryResponse>;

internal sealed class GetMostRecentlyIncrementedCounterForUserQueryHandler(
    IUserHistoryReadRepository repository)
    : IGetMostRecentlyIncrementedCounterForUserQueryHandler
{
    public static void ConfigurePipeline(IQueryPipeline<GetMostRecentlyIncrementedCounterForUserQuery, GetMostRecentlyIncrementedCounterForUserQueryResponse> pipeline) => pipeline.UseDefault();

    public async Task<GetMostRecentlyIncrementedCounterForUserQueryResponse> ExecuteQuery(GetMostRecentlyIncrementedCounterForUserQuery query, CancellationToken cancellationToken = default)
    {
        var counterName = await repository.GetMostRecentlyIncrementedCounterByUserId(query.UserId);
        return new(counterName);
    }
}
