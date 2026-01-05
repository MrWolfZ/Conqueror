namespace Conqueror;

public readonly record struct IteratorTransportType(string Name, IteratorTransportRole Role);

[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = $"this enum is an enhancement of the {nameof(IteratorTransportType)}, so it makes sense to keep it after"
)]
public enum IteratorTransportRole
{
    Client = 0,
    Server = 1,
}
