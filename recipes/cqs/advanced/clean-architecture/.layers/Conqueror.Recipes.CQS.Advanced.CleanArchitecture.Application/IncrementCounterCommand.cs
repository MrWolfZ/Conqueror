namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

[HttpCommand]
public sealed record IncrementCounterCommand([Required] string CounterName, [Required] string UserId);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>;

internal sealed class IncrementCounterCommandHandler(
    ICountersReadRepository countersReadRepository,
    ICountersWriteRepository countersWriteRepository,
    IUserHistoryWriteRepository userHistoryWriteRepository)
    : IIncrementCounterCommandHandler
{
    public static void ConfigurePipeline(ICommandPipeline<IncrementCounterCommand, IncrementCounterCommandResponse> pipeline) => pipeline.UseDefault();

    public async Task<IncrementCounterCommandResponse> Handle(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await countersReadRepository.GetCounterValue(command.CounterName);
        var newCounterValue = (counterValue ?? 0) + 1;
        await countersWriteRepository.SetCounterValue(command.CounterName, newCounterValue);
        await userHistoryWriteRepository.SetMostRecentlyIncrementedCounter(command.UserId, command.CounterName);
        return new(newCounterValue);
    }
}
