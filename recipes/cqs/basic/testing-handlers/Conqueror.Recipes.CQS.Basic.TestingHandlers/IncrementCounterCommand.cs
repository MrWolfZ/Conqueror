namespace Conqueror.Recipes.CQS.Basic.TestingHandlers;

public sealed record IncrementCounterCommand(string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>
{
}

internal sealed class IncrementCounterCommandHandler : IIncrementCounterCommandHandler
{
    private readonly CountersRepository repository;
    private readonly IAdminNotificationService adminNotificationService;

    public IncrementCounterCommandHandler(CountersRepository repository, IAdminNotificationService adminNotificationService)
    {
        this.repository = repository;
        this.adminNotificationService = adminNotificationService;
    }

    public async Task<IncrementCounterCommandResponse> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await GetCounterValue(command.CounterName);
        var newCounterValue = counterValue + 1;
        await repository.SetCounterValue(command.CounterName, newCounterValue);

        if (newCounterValue >= 1000)
        {
            // simulate a side-effect during command execution
            await adminNotificationService.SendCounterIncrementedBeyondThresholdNotification(command.CounterName);
        }

        return new(newCounterValue);
    }

    private async Task<int> GetCounterValue(string counterName)
    {
        try
        {
            return await repository.GetCounterValue(counterName);
        }
        catch (CounterNotFoundException)
        {
            return 0;
        }
    }
}
