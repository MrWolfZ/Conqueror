namespace Conqueror.Recipes.Messaging.CallingHttp.Server;

internal partial class IncrementCounterHandler(CountersRepository repository) : IncrementCounter.IHandler
{
    public static void ConfigurePipeline(IncrementCounter.IPipeline pipeline) =>
        pipeline.UseDataAnnotationValidation();

    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await repository.SetCounterValue(message.CounterName, newCounterValue);
        return new IncrementCounterResponse(newCounterValue);
    }
}
