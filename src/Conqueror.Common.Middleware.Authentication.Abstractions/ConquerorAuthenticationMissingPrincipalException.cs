using System;

namespace Conqueror;

/// <summary>
///     This exception is thrown in the context of a Conqueror execution when an authenticated
///     principal is expected to be present but is missing.
/// </summary>
[Serializable]
public sealed class ConquerorAuthenticationMissingPrincipalException : ConquerorAuthenticationFailedException
{
    public ConquerorAuthenticationMissingPrincipalException(string message)
        : base(message)
    {
    }

    public ConquerorAuthenticationMissingPrincipalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private ConquerorAuthenticationMissingPrincipalException()
    {
    }
}
