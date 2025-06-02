using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class HttpWebSocketsSignalReceiverRunFailedException : Exception
{
    public HttpWebSocketsSignalReceiverRunFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpWebSocketsSignalReceiverRunFailedException(string message)
        : base(message)
    {
    }

    private HttpWebSocketsSignalReceiverRunFailedException()
    {
    }

    public required Type HandlerType { get; init; }
}
