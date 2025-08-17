namespace Conqueror;

public readonly record struct SignalTransportType(string Name, SignalTransportRole Role);

[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = $"this enum is an enhancement of the {nameof(SignalTransportType)}, so it makes sense to keep it after"
)]
public enum SignalTransportRole
{
    Publisher,
    Receiver,
}
