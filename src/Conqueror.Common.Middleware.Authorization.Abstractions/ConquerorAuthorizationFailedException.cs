using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     This is the base exception for all conqueror authorization failures. It allows catching all kinds
///     of authorization failures without needing to know the concrete failure type.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "we have equivalent constructors with one added parameter")]
public abstract class ConquerorAuthorizationFailedException : Exception, ISerializable
{
    protected ConquerorAuthorizationFailedException(string message, ConquerorAuthorizationResult result)
        : base(message)
    {
        Result = result;
    }

    protected ConquerorAuthorizationFailedException(string message, Exception innerException, ConquerorAuthorizationResult result)
        : base(message, innerException)
    {
        Result = result;
    }

    protected ConquerorAuthorizationFailedException(ConquerorAuthorizationResult result)
    {
        Result = result;
    }

    protected ConquerorAuthorizationFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
        ArgumentNullException.ThrowIfNull(serializationInfo);

        Result = (ConquerorAuthorizationResult)serializationInfo.GetValue("Result", typeof(ConquerorAuthorizationResult))!; // Do not rename (binary serialization)
    }

    public ConquerorAuthorizationResult Result { get; }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        GetObjectData(info, context);
        
        info.AddValue("Result", Result);
    }
}
