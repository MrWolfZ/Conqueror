namespace Conqueror.Common;

internal sealed class DefaultConquerorContext : IConquerorContext
{
    public DefaultConquerorContext(string traceId)
    {
        TraceId = traceId;
    }

    public string TraceId { get; private set; }

    public IConquerorContextData DownstreamContextData { get; } = new DefaultConquerorContextData();

    public IConquerorContextData UpstreamContextData { get; } = new DefaultConquerorContextData();

    public IConquerorContextData ContextData { get; } = new DefaultConquerorContextData();

    public void SetTraceId(string traceId)
    {
        TraceId = traceId;
    }
}
