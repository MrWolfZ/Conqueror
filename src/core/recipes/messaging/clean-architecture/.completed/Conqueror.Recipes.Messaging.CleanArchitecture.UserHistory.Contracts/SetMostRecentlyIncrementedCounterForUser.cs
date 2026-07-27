namespace Conqueror.Recipes.Messaging.CleanArchitecture.UserHistory.Contracts;

// this message has no HTTP attribute: it is an internal contract used by other bounded contexts
// (e.g. Counters) to notify the UserHistory context, not something we expose to the outside world
[Message]
public partial record SetMostRecentlyIncrementedCounterForUser(string UserId, string CounterName);
