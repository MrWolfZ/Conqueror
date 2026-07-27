namespace Conqueror.Recipes.Messaging.CleanArchitecture;

[HttpMessage<GetCounterValueResponse>(HttpMethod = "GET")]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(bool CounterExists, int? CounterValue);

internal partial class GetCounterValueHandler(CountersRepository repository) : GetCounterValue.IHandler
{
    public static void ConfigurePipeline(GetCounterValue.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<GetCounterValueResponse> Handle(GetCounterValue message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        return new GetCounterValueResponse(counterValue.HasValue, counterValue);
    }
}
