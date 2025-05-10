using System;
using Conqueror.Messaging;

#pragma warning disable CA1813 // Avoid unsealed attributes; we don't want to have to repeat all properties for the generic attribute

// ReSharper disable once CheckNamespace
namespace Conqueror;

[MessageTransport(Prefix = "Http", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class HttpMessageAttribute : Attribute
{
    /// <summary>
    ///     The HTTP method for this message type. Defaults to <c>POST</c>.
    /// </summary>
    /// <example>POST, GET, DELETE, ...</example>
    public string? HttpMethod { get; set; }

    /// <summary>
    ///     The path prefix for this message type. Defaults to <c>api</c>.
    /// </summary>
    /// <example>some/custom/prefix</example>
    public string? PathPrefix { get; set; }

    /// <summary>
    ///     The path for this message type. Defaults to the type name of the
    ///     message type (with a <c>...Message</c> suffix removed if any).
    /// </summary>
    /// <example>some/custom/path</example>
    public string? Path { get; set; }

    /// <summary>
    ///     A fixed full path for this message type. Defaults to a concatenation
    ///     of <see cref="PathPrefix" />, <see cref="Version" /> (if any) and
    ///     <see cref="Path" />. If this property is set, the aforementioned
    ///     properties are ignored.
    /// </summary>
    /// <example>a/full/custom/path</example>
    public string? FullPath { get; set; }

    /// <summary>
    ///     The version of this message type.
    /// </summary>
    /// <example>v1</example>
    public string? Version { get; set; }

    /// <summary>
    ///     The HTTP status code to use for successful responses. Defaults to 200.
    /// </summary>
    public int SuccessStatusCode { get; set; }

    /// <summary>
    ///     The name of this message type in API descriptions (which is used in e.g. OpenAPI specifications).
    ///     Defaults to the type name of the message type.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     The name of the API group in which this message type is contained in API descriptions
    ///     (which is used in e.g. OpenAPI specifications).
    /// </summary>
    public string? ApiGroupName { get; set; }
}

// ReSharper disable once UnusedTypeParameter (used by source generator)
[MessageTransport(Prefix = "Http", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpMessageAttribute<TResponse> : HttpMessageAttribute;
