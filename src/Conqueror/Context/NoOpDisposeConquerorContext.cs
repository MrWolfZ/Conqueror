namespace Conqueror.Context;

internal sealed class NoOpDisposeConquerorContext(ConquerorContext wrappedContext) : ConquerorContext
{
    public override IConquerorContextData DownstreamContextData => wrappedContext.DownstreamContextData;

    public override IConquerorContextData UpstreamContextData => wrappedContext.UpstreamContextData;

    public override IConquerorContextData ContextData => wrappedContext.ContextData;

    protected override void Dispose(bool isDisposing)
    {
    }
}
