namespace Conqueror.Common;

internal sealed class DefaultConquerorContext(string traceId) : IConquerorContext
{
    public string TraceId { get; private set; } = traceId;

    public IConquerorContextData DownstreamContextData { get; } = new DefaultConquerorContextData();

    public IConquerorContextData UpstreamContextData { get; } = new DefaultConquerorContextData();

    public IConquerorContextData ContextData { get; } = new DefaultConquerorContextData();

    public void SetTraceId(string traceId)
    {
        TraceId = traceId;
    }
}
