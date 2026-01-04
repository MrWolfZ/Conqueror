namespace Conqueror.Context;

using System.Security.Claims;

internal sealed class NoOpDisposeConquerorContext(ConquerorContext wrappedContext) : ConquerorContext
{
    public override string TraceId
    {
        get => wrappedContext.TraceId;
        set => wrappedContext.TraceId = value;
    }

    public override string? MessageId
    {
        get => wrappedContext.MessageId;
        set => wrappedContext.MessageId = value;
    }

    public override string? SignalId
    {
        get => wrappedContext.SignalId;
        set => wrappedContext.SignalId = value;
    }

    public override string? IteratorId
    {
        get => wrappedContext.IteratorId;
        set => wrappedContext.IteratorId = value;
    }

    public override ClaimsPrincipal? CurrentPrincipal
    {
        get => wrappedContext.CurrentPrincipal;
        set => wrappedContext.CurrentPrincipal = value;
    }

    public override ITransportableConquerorContextData TransportableData => wrappedContext.TransportableData;

    public override IInProcessConquerorContextData InProcessData => wrappedContext.InProcessData;

    protected override void Dispose(bool isDisposing) { }
}
