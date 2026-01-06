namespace Conqueror.Recipes.Messaging.TestingHandlers;

[Message<IncrementCounterResponse>]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(
    CountersRepository repository,
    IAdminNotificationService adminNotificationService
) : IncrementCounter.IHandler
{
    public async Task<IncrementCounterResponse> Handle(
        IncrementCounter message,
        CancellationToken cancellationToken = default
    )
    {
        var counterValue = await GetCounterValue(message.CounterName);
        var newCounterValue = counterValue + 1;
        await repository.SetCounterValue(message.CounterName, newCounterValue);

        if (newCounterValue >= 1000)
        {
            await adminNotificationService.SendCounterIncrementedBeyondThresholdNotification(
                message.CounterName
            );
        }

        return new IncrementCounterResponse(newCounterValue);
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
