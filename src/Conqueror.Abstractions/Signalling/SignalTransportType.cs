// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct SignalTransportType(string Name, SignalTransportRole Role);

public enum SignalTransportRole
{
    Publisher,
    Receiver,
}
