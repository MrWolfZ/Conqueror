using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class HttpSseSignalFailedOnPublisherException : SignalFailedException
{
    public HttpSseSignalFailedOnPublisherException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpSseSignalFailedOnPublisherException(string message)
        : base(message)
    {
    }

    private HttpSseSignalFailedOnPublisherException()
    {
    }

    public override string WellKnownReason => WellKnownReasons.None;
}
