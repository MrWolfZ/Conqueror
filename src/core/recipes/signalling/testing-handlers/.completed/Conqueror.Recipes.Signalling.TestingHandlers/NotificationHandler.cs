namespace Conqueror.Recipes.Signalling.TestingHandlers;

internal partial class NotificationHandler(IAdminNotificationService adminNotificationService)
    : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        if (signal.NewValue >= 1000)
        {
            await adminNotificationService.SendCounterIncrementedBeyondThresholdNotification(
                signal.CounterName
            );
        }
    }
}
