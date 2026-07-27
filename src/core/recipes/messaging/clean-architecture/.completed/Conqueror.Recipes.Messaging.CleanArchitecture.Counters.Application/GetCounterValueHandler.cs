namespace Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Application;

internal partial class GetCounterValueHandler(ICountersReadRepository repository) : GetCounterValue.IHandler
{
    public static void ConfigurePipeline(GetCounterValue.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<GetCounterValueResponse> Handle(GetCounterValue message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        return new GetCounterValueResponse(counterValue.HasValue, counterValue);
    }
}
