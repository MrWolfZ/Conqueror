namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

internal sealed class IncrementCounterCommandHandler : IIncrementCounterCommandHandler
{
    private readonly CountersRepository repository;

    public IncrementCounterCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline.UseDataAnnotationValidation();

    public async Task<IncrementCounterCommandResponse> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(command.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await repository.SetCounterValue(command.CounterName, newCounterValue);
        return new(newCounterValue);
    }
}
