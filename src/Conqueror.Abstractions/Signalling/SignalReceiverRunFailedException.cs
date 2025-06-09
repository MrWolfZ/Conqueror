using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class SignalReceiverRunFailedException : Exception
{
    public SignalReceiverRunFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SignalReceiverRunFailedException(string message)
        : base(message)
    {
    }

    private SignalReceiverRunFailedException()
    {
    }

    public required Type HandlerType { get; init; }

    public required SignalTransportType SignalTransportType { get; init; }
}
