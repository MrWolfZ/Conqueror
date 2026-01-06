namespace Conqueror.Recipes.Messaging.TestingHandlers;

public interface IAdminNotificationService
{
    Task SendCounterIncrementedBeyondThresholdNotification(string counterName);
}

internal class NoopAdminNotificationService : IAdminNotificationService
{
    public Task SendCounterIncrementedBeyondThresholdNotification(string counterName)
    {
        return Task.CompletedTask;
    }
}
