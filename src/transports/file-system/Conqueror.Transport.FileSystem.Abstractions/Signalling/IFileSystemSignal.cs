namespace Conqueror;

[SuppressMessage(
    "ReSharper",
    "StaticMemberInGenericType",
    Justification = "The static members are intentionally per generic type"
)]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IFileSystemSignal<TSignal> : ISignal<TSignal>
    where TSignal : class, IFileSystemSignal<TSignal>
{
    /// <summary>
    ///     This tag is used to identify different signal types in the same connection.
    /// </summary>
    static virtual string Tag { get; } =
        $"{Dasherize(typeof(TSignal).Name.EndsWith("Signal", StringComparison.Ordinal) ? typeof(TSignal).Name[..^6] : typeof(TSignal).Name)}";

    static virtual JsonSerializerContext? FileSystemJsonSerializerContext => TSignal.JsonSerializerContext;

    internal static virtual IFileSystemSignalSerializer<TSignal> FileSystemSignalSerializer { get; } =
        new FileSystemSignalJsonSerializer<TSignal>();

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
                _ = sb.Append(value: '-').Append(char.ToLower(str[i], CultureInfo.InvariantCulture));
            }
            else
            {
                _ = sb.Append(str[i]);
            }
        }

        return sb.ToString();
    }
}
