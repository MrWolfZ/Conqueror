using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IHttpMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    static virtual string HttpMethod => ConquerorTransportHttpConstants.MethodPost;

    static virtual string PathPrefix => "api";

    static virtual string Path { get; } = $"{Uncapitalize(typeof(TMessage).Name.EndsWith("Message") ? typeof(TMessage).Name[..^7] : typeof(TMessage).Name)}";

    static virtual string FullPath { get; } =
        $"{TMessage.PathPrefix.Trim('/')}{(TMessage.Version is { } v ? $"/{v.Trim('/')}" : string.Empty)}/{TMessage.Path.Trim('/')}";

    static virtual string? Version { get; }

    static virtual int SuccessStatusCode => typeof(TResponse) == typeof(UnitMessageResponse) ? 204 : 200;

    static virtual string Name { get; } = typeof(TMessage).Name;

    static virtual string? ApiGroupName { get; }

    static virtual JsonSerializerContext? HttpJsonSerializerContext => TMessage.JsonSerializerContext;

    static virtual IHttpMessageSerializer<TMessage, TResponse>? HttpMessageSerializer
        => TMessage.HttpMethod == ConquerorTransportHttpConstants.MethodGet
            ? new HttpMessageQueryStringSerializer<TMessage, TResponse>()
            : null;

    static virtual IHttpMessageResponseSerializer<TMessage, TResponse>? HttpMessageResponseSerializer => null;

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}
