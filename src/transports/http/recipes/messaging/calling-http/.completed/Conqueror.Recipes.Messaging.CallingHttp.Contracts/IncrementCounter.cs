namespace Conqueror.Recipes.Messaging.CallingHttp.Contracts;

[HttpMessage<IncrementCounterResponse>(Version = "v1")]
public partial record IncrementCounter(string CounterName)
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "counter name must not be empty")]
    public string CounterName { get; } = CounterName;
}

public record IncrementCounterResponse(int NewCounterValue);
