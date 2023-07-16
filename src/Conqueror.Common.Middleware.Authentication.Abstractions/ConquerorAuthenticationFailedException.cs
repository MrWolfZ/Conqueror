using System;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     This is the base exception for all conqueror authentication failures. It allows catching all kinds
///     of authentication failures without needing to know the concrete failure type.
/// </summary>
[Serializable]
public abstract class ConquerorAuthenticationFailedException : Exception
{
    protected ConquerorAuthenticationFailedException(string message)
        : base(message)
    {
    }

    protected ConquerorAuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected ConquerorAuthenticationFailedException()
    {
    }

    protected ConquerorAuthenticationFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
