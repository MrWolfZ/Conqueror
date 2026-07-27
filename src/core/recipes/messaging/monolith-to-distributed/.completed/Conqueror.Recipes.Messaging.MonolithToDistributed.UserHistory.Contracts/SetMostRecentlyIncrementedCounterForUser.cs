namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts;

// this message is exposed via HTTP so that other services (e.g. the Counters web app)
// can call it across the service boundary
[HttpMessage(HttpMethod = "POST")]
public partial record SetMostRecentlyIncrementedCounterForUser(string UserId, string CounterName);
