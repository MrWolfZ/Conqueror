using System;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     This exception is thrown in the context of a Conqueror execution when an authenticated
///     principal is expected to be present but the principal is unauthenticated.
/// </summary>
[Serializable]
public sealed class ConquerorAuthenticationUnauthenticatedPrincipalException : ConquerorAuthenticationFailedException
{
    public ConquerorAuthenticationUnauthenticatedPrincipalException(string message)
        : base(message)
    {
    }

    public ConquerorAuthenticationUnauthenticatedPrincipalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private ConquerorAuthenticationUnauthenticatedPrincipalException()
    {
    }

    private ConquerorAuthenticationUnauthenticatedPrincipalException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
