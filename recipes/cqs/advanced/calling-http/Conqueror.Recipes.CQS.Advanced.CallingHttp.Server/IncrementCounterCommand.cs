namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Server;

[HttpCommand(Version = "v1")]
public sealed record IncrementCounterCommand([Required] string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>
{
}

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
