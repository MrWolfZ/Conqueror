#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client code without an extra import

namespace Conqueror;

public enum PayloadLoggingStrategy
{
    Omit,
    Raw,
    MinimalJson,
    IndentedJson,
}
