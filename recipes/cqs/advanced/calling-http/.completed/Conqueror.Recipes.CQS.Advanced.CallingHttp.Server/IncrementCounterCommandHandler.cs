namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

internal sealed class IncrementCounterCommandHandler(CountersRepository repository) : IIncrementCounterCommandHandler
{
    public static void ConfigurePipeline(ICommandPipeline<IncrementCounterCommand, IncrementCounterCommandResponse> pipeline) => pipeline.UseDataAnnotationValidation();

    public async Task<IncrementCounterCommandResponse> Handle(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(command.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await repository.SetCounterValue(command.CounterName, newCounterValue);
        return new(newCounterValue);
    }
}
