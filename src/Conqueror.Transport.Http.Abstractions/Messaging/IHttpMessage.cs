using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
public interface IHttpMessage<TMessage, TResponse> : IHttpMessage, IMessage<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    static virtual string HttpMethod => ConquerorTransportHttpConstants.MethodPost;

    static virtual string PathPrefix => "api";

    static virtual string Path { get; } = $"{Uncapitalize(typeof(TMessage).Name.EndsWith("Message") ? typeof(TMessage).Name[..^7] : typeof(TMessage).Name)}";

    /// <summary>
    ///     The full path for this message.
    ///     Defaults to '/<see cref="PathPrefix" />[/<see cref="Version" />]/<see cref="Path" />'.
    /// </summary>
    /// <example>/api/v1/myMessage</example>
    static virtual string FullPath { get; } = $"{TMessage.PathPrefix.Trim('/')}{(TMessage.Version is { } v ? $"/{v.Trim('/')}" : string.Empty)}/{TMessage.Path.Trim('/')}";

    /// <summary>
    ///     The version of this message.
    /// </summary>
    static virtual string? Version { get; }

    static virtual int SuccessStatusCode => 200;

    /// <summary>
    ///     The name of this message in API descriptions (which is used in e.g. OpenAPI specifications).
    ///     Defaults to the type name of the message type.
    /// </summary>
    static virtual string Name { get; } = typeof(TMessage).Name;

    /// <summary>
    ///     The name of the API group in which this message is contained in API descriptions
    ///     (which is used in e.g. OpenAPI specifications and the Swagger UI).
    /// </summary>
    static virtual string? ApiGroupName { get; }

    static virtual JsonSerializerContext? HttpJsonSerializerContext => TMessage.JsonSerializerContext;

    static virtual IHttpMessageSerializer<TMessage, TResponse>? HttpMessageSerializer => null;

    static virtual IHttpResponseSerializer<TMessage, TResponse>? HttpResponseSerializer => null;

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
public interface IHttpMessage<TMessage> : IHttpMessage<TMessage, UnitMessageResponse>
    where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
{
    static int IHttpMessage<TMessage, UnitMessageResponse>.SuccessStatusCode => 204;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpMessage
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    static virtual IHttpMessageTypesInjector HttpMessageTypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}
