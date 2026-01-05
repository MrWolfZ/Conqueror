namespace Conqueror;

using Conqueror.Iterating;

/// <summary>
///     An iterator transport that uses HTTP web sockets to stream items between client and server.
/// </summary>
[IteratorTransport(Prefix = "HttpWebSockets", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpWebSocketsIteratorAttribute : Attribute
{
    /// <summary>
    ///     The path prefix for this iterator type. Defaults to <c>api/iterators</c>.
    /// </summary>
    /// <example>some/custom/prefix</example>
    public string? PathPrefix { get; set; }

    /// <summary>
    ///     The path for this iterator type. Defaults to the type name of the
    ///     iterator type (with an <c>...Iterator</c> suffix removed if any).
    /// </summary>
    /// <example>some/custom/path</example>
    public string? Path { get; set; }

    /// <summary>
    ///     A fixed full path for this iterator type. Defaults to a concatenation
    ///     of <see cref="PathPrefix" />, <see cref="Version" /> (if any) and
    ///     <see cref="Path" />. If this property is set, the aforementioned
    ///     properties are ignored.
    /// </summary>
    /// <example>a/full/custom/path</example>
    public string? FullPath { get; set; }

    /// <summary>
    ///     The version of this iterator type.
    /// </summary>
    /// <example>v1</example>
    public string? Version { get; set; }

    /// <summary>
    ///     The name of the API group in which this iterator type is contained in API descriptions
    ///     (which is used in e.g. OpenAPI specifications).
    /// </summary>
    public string? ApiGroupName { get; set; }
}
