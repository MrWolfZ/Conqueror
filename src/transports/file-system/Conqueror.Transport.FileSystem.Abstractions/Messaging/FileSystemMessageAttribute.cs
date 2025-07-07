using Conqueror.Messaging;

#pragma warning disable CA1813 // Avoid unsealed attributes; we don't want to have to repeat all properties for the generic attribute

// ReSharper disable once CheckNamespace
namespace Conqueror;

[MessageTransport(Prefix = "FileSystem", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FileSystemMessageAttribute : Attribute
{
    /// <summary>
    ///     The tag for this message type. Defaults to the dasherized type name
    ///     of the message type (with a <c>...Message</c> suffix removed if any).
    /// </summary>
    /// <example>my-message-tag</example>
    public string? Tag { get; set; }

    /// <summary>
    ///     The version of this message type.
    /// </summary>
    /// <example>v1</example>
    public string? Version { get; set; }
}

// ReSharper disable once UnusedTypeParameter (used by source generator)
[MessageTransport(Prefix = "FileSystem", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FileSystemMessageAttribute<TResponse> : FileSystemMessageAttribute;
