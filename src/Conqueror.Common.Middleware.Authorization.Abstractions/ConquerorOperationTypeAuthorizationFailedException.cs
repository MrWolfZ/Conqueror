using System;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

/// <summary>
///     This exception is thrown in the context of a Conqueror execution when the authenticated principal
///     is not authorized to execute the requested operation type.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "we have equivalent constructors with one added parameter")]
public sealed class ConquerorOperationTypeAuthorizationFailedException : ConquerorAuthorizationFailedException
{
    public ConquerorOperationTypeAuthorizationFailedException(string message, ConquerorAuthorizationResult result)
        : base(message, result)
    {
    }

    public ConquerorOperationTypeAuthorizationFailedException(string message, Exception innerException, ConquerorAuthorizationResult result)
        : base(message, innerException, result)
    {
    }

    public ConquerorOperationTypeAuthorizationFailedException(ConquerorAuthorizationResult result)
        : base(result)
    {
    }
}
