namespace Conqueror.Common;

internal sealed class DefaultConquerorContext : IConquerorContext
{
    public IConquerorContextData DownstreamContextData { get; } = new DefaultConquerorContextData();

    public IConquerorContextData UpstreamContextData { get; } = new DefaultConquerorContextData();

    public IConquerorContextData ContextData { get; } = new DefaultConquerorContextData();
}
