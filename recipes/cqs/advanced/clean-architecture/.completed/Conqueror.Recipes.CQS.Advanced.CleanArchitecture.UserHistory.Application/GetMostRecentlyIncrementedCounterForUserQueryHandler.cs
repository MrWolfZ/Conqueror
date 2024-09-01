namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;

internal sealed class GetMostRecentlyIncrementedCounterForUserQueryHandler : IGetMostRecentlyIncrementedCounterForUserQueryHandler
{
    private readonly IUserHistoryReadRepository repository;

    public GetMostRecentlyIncrementedCounterForUserQueryHandler(IUserHistoryReadRepository repository)
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
