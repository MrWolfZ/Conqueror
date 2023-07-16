using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     This exception is thrown in the context of a Conqueror execution when the authenticated principal
///     is not authorized to execute the requested operation.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "we have equivalent constructors with one added parameter")]
public sealed class ConquerorFunctionalAuthorizationFailedException : ConquerorAuthorizationFailedException
{
    public ConquerorFunctionalAuthorizationFailedException(string message, ConquerorAuthorizationResult result)
        : base(message, result)
    {
    }

    public ConquerorFunctionalAuthorizationFailedException(string message, Exception innerException, ConquerorAuthorizationResult result)
        : base(message, innerException, result)
    {
    }

    public ConquerorFunctionalAuthorizationFailedException(ConquerorAuthorizationResult result)
        : base(result)
    {
    }

    private ConquerorFunctionalAuthorizationFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
