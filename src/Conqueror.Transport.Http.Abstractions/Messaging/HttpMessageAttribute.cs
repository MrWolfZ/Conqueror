using System;
using Conqueror.Messaging;

#pragma warning disable CA1813 // Avoid unsealed attributes; we don't want to have to repeat all properties for the generic attribute

// ReSharper disable once CheckNamespace
namespace Conqueror;

[MessageTransport(Prefix = "Http", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class HttpMessageAttribute : ConquerorMessageTransportAttribute
{
    public string? HttpMethod { get; set; }

    public string? PathPrefix { get; set; }

    /// <summary>
    ///     The path for this message.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     A fixed full path for this message.
    /// </summary>
    public string? FullPath { get; set; }

    /// <summary>
    ///     The version of this message.
    /// </summary>
    public string? Version { get; set; }

    public int SuccessStatusCode { get; set; }

    /// <summary>
    ///     The name of this message in API descriptions (which is used in e.g. OpenAPI specifications).
    ///     Defaults to the type name of the message type.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     The name of the API group in which this message is contained in API descriptions
    ///     (which is used in e.g. OpenAPI specifications).
    /// </summary>
    public string? ApiGroupName { get; set; }
}

// ReSharper disable once UnusedTypeParameter (used by source generator)
[MessageTransport(Prefix = "Http", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpMessageAttribute<TResponse> : HttpMessageAttribute;
