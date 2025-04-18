using System.Diagnostics;

namespace Conqueror.Eventing;

internal sealed class DefaultEventNotificationIdFactory : IEventNotificationIdFactory
{
    public string GenerateId() => ActivitySpanId.CreateRandom().ToString();
}
