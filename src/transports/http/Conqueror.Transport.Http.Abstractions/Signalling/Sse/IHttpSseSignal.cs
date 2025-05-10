using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IHttpSseSignal<TSignal> : ISignal<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    static virtual string EventType { get; } = $"{Uncapitalize(typeof(TSignal).Name.EndsWith("Signal") ? typeof(TSignal).Name[..^6] : typeof(TSignal).Name)}";

    static virtual JsonSerializerContext? HttpSseJsonSerializerContext => TSignal.JsonSerializerContext;

    internal static virtual IHttpSseSignalSerializer<TSignal> HttpSseSignalSerializer { get; } = new HttpSseSignalJsonSerializer<TSignal>();

    private static string Uncapitalize(string str)
        => char.ToLower(str[0], CultureInfo.InvariantCulture) + str[1..];
}
