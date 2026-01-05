namespace Conqueror;

public readonly record struct MessageTransportType(string Name, MessageTransportRole Role);

[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = $"this enum is an enhancement of the {nameof(MessageTransportType)}, so it makes sense to keep it after"
)]
public enum MessageTransportRole
{
    Sender = 0,
    Receiver = 1,
}
