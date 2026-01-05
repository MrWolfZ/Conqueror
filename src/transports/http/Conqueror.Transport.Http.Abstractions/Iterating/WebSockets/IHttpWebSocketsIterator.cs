namespace Conqueror;

using Conqueror.Iterating.WebSockets;

[SuppressMessage(
    "ReSharper",
    "StaticMemberInGenericType",
    Justification = "The static members are intentionally per generic type"
)]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IHttpWebSocketsIterator<TIterator, TItem> : IIterator<TIterator, TItem>
    where TIterator : class, IHttpWebSocketsIterator<TIterator, TItem>
{
    static virtual string PathPrefix => "api/iterators";

    static virtual string Path { get; } =
        $"{Uncapitalize(typeof(TIterator).Name.EndsWith("Iterator", StringComparison.Ordinal) ? typeof(TIterator).Name[..^8] : typeof(TIterator).Name)}";

    static virtual string FullPath { get; } =
        $"{TIterator.PathPrefix.Trim(trimChar: '/')}{(TIterator.Version is { } v ? $"/{v.Trim(trimChar: '/')}" : "")}/{TIterator.Path.Trim(trimChar: '/')}";

    static virtual string? Version { get; }

    static virtual string Name { get; } = typeof(TIterator).Name;

    static virtual string? ApiGroupName { get; }

    static virtual JsonSerializerContext? HttpWebSocketsIteratorJsonSerializerContext =>
        TIterator.JsonSerializerContext;

    static virtual JsonSerializerContext? HttpWebSocketsItemJsonSerializerContext => TIterator.JsonSerializerContext;

    internal static virtual IHttpWebSocketsIteratorSerializer<TIterator> HttpWebSocketsIteratorSerializer { get; } =
        new HttpWebSocketsIteratorJsonSerializer<TIterator, TItem>();

    internal static virtual IHttpWebSocketsItemSerializer<TItem> HttpWebSocketsItemSerializer { get; } =
        new HttpWebSocketsIteratorItemJsonSerializer<TIterator, TItem>();

    private static string Uncapitalize(string str) => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}
