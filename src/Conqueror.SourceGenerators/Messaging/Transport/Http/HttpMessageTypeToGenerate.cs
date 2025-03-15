namespace Conqueror.SourceGenerators.Messaging.Transport.Http;

public readonly record struct HttpMessageTypeToGenerate(
    MessageTypeToGenerate MessageTypeToGenerate,
    string? HttpMethod,
    string? PathPrefix,
    string? Path,
    string? FullPath,
    string? Version,
    int? SuccessStatusCode,
    string? Name,
    string? ApiGroupName)
{
    public readonly MessageTypeToGenerate MessageTypeToGenerate = MessageTypeToGenerate;
    public readonly string? HttpMethod = HttpMethod;
    public readonly string? PathPrefix = PathPrefix;
    public readonly string? Path = Path;
    public readonly string? FullPath = FullPath;
    public readonly string? Version = Version;
    public readonly int? SuccessStatusCode = SuccessStatusCode;
    public readonly string? Name = Name;
    public readonly string? ApiGroupName = ApiGroupName;
}
