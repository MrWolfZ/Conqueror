namespace Conqueror.Recipes.CQS.Basics.TestingHandlers;

public interface IAdminNotificationService
{
    Task SendCounterIncrementedBeyondThresholdNotification(string counterName);
}

internal sealed class NoopAdminNotificationService : IAdminNotificationService
{
    public Task SendCounterIncrementedBeyondThresholdNotification(string counterName)
    {
        // in a real app, you would send a notification here
        return Task.CompletedTask;
    }
}
