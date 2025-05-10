namespace Conqueror.Transport.Http.Tests;

internal static class HttpTestContextData
{
    public static readonly Dictionary<string, string> ContextData = new()
    {
        { "key1", "value1" },
        { "key2", "value2" },
        { "keyWith,Comma", "value" },
        { "key4", "valueWith,Comma" },
        { "keyWith=Equals", "value" },
        { "key6", "valueWith=Equals" },
        { "keyWith|Pipe", "value" },
        { "key8", "valueWith|Pipe" },
        { "keyWith:Colon", "value" },
        { "key10", "valueWith:Colon" },
    };

    public static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key11", "value1" },
        { "key12", "value2" },
    };
}
