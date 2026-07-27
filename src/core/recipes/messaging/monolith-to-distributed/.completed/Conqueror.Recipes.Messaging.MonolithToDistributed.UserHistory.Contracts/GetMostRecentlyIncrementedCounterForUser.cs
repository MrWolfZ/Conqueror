namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts;

[HttpMessage<GetMostRecentlyIncrementedCounterForUserResponse>(HttpMethod = "GET")]
public partial record GetMostRecentlyIncrementedCounterForUser(string UserId);

public record GetMostRecentlyIncrementedCounterForUserResponse(string? CounterName);
