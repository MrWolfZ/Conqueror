namespace Conqueror.Transport.ConformityTests;

internal static class TestContextData
{
    private static readonly Dictionary<string, string> BaseContextData = new()
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

    public static readonly Dictionary<string, string> ContextDataDownstreamAcrossTransports
        = new(BaseContextData.Select(p => new KeyValuePair<string, string>(p.Key + "_downstream", p.Value + "_downstream")));

    public static readonly Dictionary<string, string> ContextDataUpstreamAcrossTransports
        = new(BaseContextData.Select(p => new KeyValuePair<string, string>(p.Key + "_upstream", p.Value + "_upstream")));

    public static readonly Dictionary<string, string> ContextDataDownstreamBidirectionalAcrossTransports
        = new(BaseContextData.Select(p => new KeyValuePair<string, string>(p.Key + "_downstream_bidirectional", p.Value + "_downstream_bidirectional")));

    public static readonly Dictionary<string, string> ContextDataUpstreamBidirectionalAcrossTransports
        = new(BaseContextData.Select(p => new KeyValuePair<string, string>(p.Key + "_upstream_bidirectional", p.Value + "_upstream_bidirectional")));

    public static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key1_in_process", "value1" },
        { "key2_in_process", "value2" },
    };
}
