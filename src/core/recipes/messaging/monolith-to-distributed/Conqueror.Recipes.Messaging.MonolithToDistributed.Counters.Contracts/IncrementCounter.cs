namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Contracts;

[HttpMessage<IncrementCounterResponse>(HttpMethod = "POST")]
public partial record IncrementCounter(string CounterName, string UserId);

public record IncrementCounterResponse(int NewCounterValue);
