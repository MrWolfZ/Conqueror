namespace Conqueror;

[SuppressMessage(
    "ReSharper",
    "StaticMemberInGenericType",
    Justification = "The static members are intentionally per generic type"
)]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IHttpWebSocketsSignal<TSignal> : ISignal<TSignal>
    where TSignal : class, IHttpWebSocketsSignal<TSignal>
{
    /// <summary>
    ///     This tag is used to identify different signal types in the same connection.
    /// </summary>
    static virtual string Tag { get; } =
        $"{Uncapitalize(typeof(TSignal).Name.EndsWith("Signal", StringComparison.Ordinal) ? typeof(TSignal).Name[..^6] : typeof(TSignal).Name)}";

    static virtual JsonSerializerContext? HttpWebSocketsJsonSerializerContext => TSignal.JsonSerializerContext;

    internal static virtual IHttpWebSocketsSignalSerializer<TSignal> HttpWebSocketsSignalSerializer { get; } =
        new HttpWebSocketsSignalJsonSerializer<TSignal>();

    private static string Uncapitalize(string str) => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}
