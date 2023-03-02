namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;

[HttpQuery]
public sealed record GetMostRecentlyIncrementedCounterForUserQuery([Required] string UserId);

public sealed record GetMostRecentlyIncrementedCounterForUserQueryResponse(string? CounterName);

public interface IGetMostRecentlyIncrementedCounterForUserQueryHandler : IQueryHandler<GetMostRecentlyIncrementedCounterForUserQuery, GetMostRecentlyIncrementedCounterForUserQueryResponse>
{
}
