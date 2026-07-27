namespace Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Contracts;

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
