using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     This exception is thrown in the context of a Conqueror execution when the authenticated principal
///     is not authorized to access the requested data.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "we have equivalent constructors with one added parameter")]
public sealed class ConquerorDataAuthorizationFailedException : ConquerorAuthorizationFailedException
{
    public ConquerorDataAuthorizationFailedException(string message, ConquerorAuthorizationResult result)
        : base(message, result)
    {
    }

    public ConquerorDataAuthorizationFailedException(string message, Exception innerException, ConquerorAuthorizationResult result)
        : base(message, innerException, result)
    {
    }

    public ConquerorDataAuthorizationFailedException(ConquerorAuthorizationResult result)
        : base(result)
    {
    }

    private ConquerorDataAuthorizationFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
