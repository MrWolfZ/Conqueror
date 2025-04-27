// ReSharper disable once CheckNamespace (we want this to be accessible from client code without an extra import)
namespace Conqueror;

public enum PayloadLoggingStrategy
{
    Omit,
    Raw,
    MinimalJson,
    IndentedJson,
}
