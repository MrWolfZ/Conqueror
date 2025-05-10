using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class HttpSseSignalReceiverRunFailedException : Exception
{
    public HttpSseSignalReceiverRunFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpSseSignalReceiverRunFailedException(string message)
        : base(message)
    {
    }

    private HttpSseSignalReceiverRunFailedException()
    {
    }

    public required Type HandlerType { get; init; }
}
