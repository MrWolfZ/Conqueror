// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct MessageTransportType(string Name, MessageTransportRole Role);

public enum MessageTransportRole
{
    Sender,
    Receiver,
}
