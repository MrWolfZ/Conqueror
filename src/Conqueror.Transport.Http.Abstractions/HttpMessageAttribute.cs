using System;

namespace Conqueror;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpMessageAttribute : Attribute
{
    public string? HttpMethod { get; set; }

    public string? PathPrefix { get; set; }

    /// <summary>
    ///     A fixed path for this message.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     The version of this message.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    ///     The operation ID of this message in API descriptions (which is used in e.g. OpenAPI specifications).
    ///     Defaults to the full type name of the message type.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    ///     The name of the API group in which this message is contained in API descriptions
    ///     (which is used in e.g. OpenAPI specifications).
    /// </summary>
    public string? ApiGroupName { get; set; }
}
