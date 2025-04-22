using Conqueror.SourceGenerators.Util.Messaging;

namespace Conqueror.Transport.Http.SourceGenerators.Messaging;

public readonly record struct HttpMessageTypesDescriptor(
    MessageTypesDescriptor MessageTypesDescriptor,
    string? HttpMethod,
    string? PathPrefix,
    string? Path,
    string? FullPath,
    string? Version,
    int? SuccessStatusCode,
    string? Name,
    string? ApiGroupName)
{
    public readonly string? ApiGroupName = ApiGroupName;
    public readonly string? FullPath = FullPath;
    public readonly string? HttpMethod = HttpMethod;
    public readonly MessageTypesDescriptor MessageTypesDescriptor = MessageTypesDescriptor;
    public readonly string? Name = Name;
    public readonly string? Path = Path;
    public readonly string? PathPrefix = PathPrefix;
    public readonly int? SuccessStatusCode = SuccessStatusCode;
    public readonly string? Version = Version;
}
