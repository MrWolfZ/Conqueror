// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed record SignalTransportType(string Name, SignalTransportRole Role);

public enum SignalTransportRole
{
    Publisher,
    Receiver,
}
