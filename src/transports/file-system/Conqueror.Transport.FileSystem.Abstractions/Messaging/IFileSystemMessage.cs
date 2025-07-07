using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "The static members are intentionally per generic type")]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IFileSystemMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    static virtual string Tag { get; } = $"{Dasherize(typeof(TMessage).Name.EndsWith("Message") ? typeof(TMessage).Name[..^7] : typeof(TMessage).Name)}";

    static virtual string? Version { get; }

    static virtual JsonSerializerContext? FileSystemJsonSerializerContext => TMessage.JsonSerializerContext;

    internal static virtual string FullTag { get; } = $"{AppendVersion(TMessage.Tag)}";

    internal static virtual IFileSystemMessageSerializer<TMessage, TResponse> FileSystemMessageSerializer
        => FileSystemMessageJsonSerializer<TMessage, TResponse>.Default;

    internal static virtual IFileSystemMessageResponseSerializer<TMessage, TResponse> FileSystemMessageResponseSerializer
        => FileSystemMessageResponseJsonSerializer<TMessage, TResponse>.Default;

    private static string Dasherize(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var sb = new StringBuilder(str.Length + 5);
        _ = sb.Append(char.ToLower(str[0], CultureInfo.InvariantCulture));

        for (var i = 1; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]))
            {
                _ = sb.Append('-')
                      .Append(char.ToLower(str[i], CultureInfo.InvariantCulture));
            }
            else
            {
                _ = sb.Append(str[i]);
            }
        }

        return sb.ToString();
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "we expect lower-case to not be a problem")]
    private static string AppendVersion(string str)
        => TMessage.Version is null || str.EndsWith(TMessage.Version, StringComparison.OrdinalIgnoreCase)
            ? str
            : $"{str}-{TMessage.Version.ToLower(CultureInfo.InvariantCulture)}";
}
