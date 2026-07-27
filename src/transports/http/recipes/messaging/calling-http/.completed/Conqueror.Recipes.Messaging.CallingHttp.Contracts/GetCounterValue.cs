namespace Conqueror.Recipes.Messaging.CallingHttp.Contracts;

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET", Version = "v1")]
public partial record GetCounterValue(string CounterName)
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "counter name must not be empty")]
    public string CounterName { get; } = CounterName;
}

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);
