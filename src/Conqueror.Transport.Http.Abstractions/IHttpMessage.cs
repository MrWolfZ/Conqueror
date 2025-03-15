using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Conqueror;

public interface IHttpMessage;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
public interface IHttpMessage<TMessage> : IHttpMessage, IHttpMessageWithTypesInjectionFactory
    where TMessage : IHttpMessage<TMessage>
{
    static virtual string HttpMethod { get; } = "POST";

    static virtual string PathPrefix { get; } = "/api";

    static virtual string Path { get; } = $"/{Uncapitalize(typeof(TMessage).Name.EndsWith("Message") ? typeof(TMessage).Name[..^7] : typeof(TMessage).Name)}";

    /// <summary>
    ///     The full path for this message.
    ///     Defaults to '/<see cref="PathPrefix" />[/<see cref="Version" />]/<see cref="Path" />'.
    /// </summary>
    /// <example>/api/v1/myMessage</example>
    static virtual string? FullPath { get; } = $"/{TMessage.PathPrefix}{(TMessage.Version is { } v ? $"{v}/" : string.Empty)}{TMessage.Path}";

    /// <summary>
    ///     The version of this message.
    /// </summary>
    static virtual string? Version { get; }

    /// <summary>
    ///     The name of this message in API descriptions (which is used in e.g. OpenAPI specifications).
    ///     Defaults to the type name of the message type.
    /// </summary>
    static virtual string Name { get; } = typeof(TMessage).Name;

    /// <summary>
    ///     The operation ID of this message in API descriptions (which is used in e.g. OpenAPI specifications).
    ///     Defaults to the full type name of the message type.
    /// </summary>
    static virtual string OperationId { get; } = typeof(TMessage).FullName ?? TMessage.Name;

    /// <summary>
    ///     The name of the API group in which this message is contained in API descriptions
    ///     (which is used in e.g. OpenAPI specifications and the Swagger UI).
    /// </summary>
    static virtual string? ApiGroupName { get; }

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessageWithTypesInjectionFactory
{
    /// <summary>
    ///     Helper method to be able to access the message and response types as generic parameters while only
    ///     having a generic reference to the message type. This allows bypassing reflection.
    /// </summary>
    /// <param name="factory">The factory that should be called with the generic type parameters</param>
    /// <typeparam name="TResult">The type of result the factory will return</typeparam>
    /// <returns>The result of calling the factory</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjectionFactory<TResult> factory);
}
