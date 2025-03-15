using System;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public abstract class ConquerorMessageFailedOnClientException : Exception
{
    protected ConquerorMessageFailedOnClientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    private ConquerorMessageFailedOnClientException()
    {
    }
}
