using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

namespace Conqueror.Context;

internal sealed class DefaultConquerorContext : ConquerorContext,

                                                // performance: we implement these interfaces directly here instead of always delegating to
                                                // DefaultConquerorContextData to prevent unnecessary allocations of the context data
                                                // objects when users have added no data (i.e. we can shortcut the `GetAll` calls in that
                                                // case)
                                                ITransportableConquerorContextData,
                                                IInProcessConquerorContextData
{
    private readonly Action<DefaultConquerorContext> onDispose;
    private readonly DefaultConquerorContext? parent;

    private DefaultConquerorContextData? contextData;

    private DefaultConquerorContext(string traceId, Action<ConquerorContext> onDispose)
    {
        TraceId = traceId;

        this.onDispose = onDispose;
        parent = null;
    }

    private DefaultConquerorContext(DefaultConquerorContext parent, Action<DefaultConquerorContext> onDispose)
    {
        TraceId = parent.TraceId;
        MessageId = parent.MessageId;
        SignalId = parent.SignalId;
        CurrentPrincipal = parent.CurrentPrincipal;

        this.onDispose = onDispose;
        this.parent = parent;

        if (parent.contextData is not null)
        {
            contextData = new(parent.contextData);
        }
    }

    public override string TraceId { get; set; }

    public override string? MessageId { get; set; }

    public override string? SignalId { get; set; }

    public override ClaimsPrincipal? CurrentPrincipal { get; set; }

    public override ITransportableConquerorContextData TransportableData => this;

    public override IInProcessConquerorContextData InProcessData => this;

    private DefaultConquerorContextData ContextData => LazyInitializer.EnsureInitialized(ref contextData, static () => new());

    private ITransportableConquerorContextData TransportableContextData => ContextData;

    private IInProcessConquerorContextData InProcessContextData => ContextData;

    public static DefaultConquerorContext CreateRootContext(string traceId, Action<ConquerorContext> onRootDispose)
    {
        return new(traceId, onRootDispose);
    }

    public DefaultConquerorContext CreateChildContext(Action onChildDispose)
    {
        return new(
            this,
            ctx =>
            {
                PropagateUpstreamData(ctx);
                onChildDispose();
            });
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            parent?.PropagateUpstreamData(this);

            onDispose(this);
        }
    }

    private void PropagateUpstreamData(DefaultConquerorContext childContext)
    {
        if (childContext.contextData is not null)
        {
            ContextData.PropagateUpstreamData(childContext.contextData);
        }
    }

    #region data interface members

#pragma warning disable SA1202 // Elements must be ordered by access

    bool ITransportableConquerorContextData.Add(string key, string value, ConquerorContextDataFlowDirection flowDirection)
        => TransportableContextData.Add(key, value, flowDirection);

    void ITransportableConquerorContextData.Set(string key, string value, ConquerorContextDataFlowDirection flowDirection)
        => TransportableContextData.Set(key, value, flowDirection);

    bool ITransportableConquerorContextData.Remove(string key, ConquerorContextDataFlowDirection flowDirection)
        => contextData is not null && TransportableContextData.Remove(key, flowDirection);

    void ITransportableConquerorContextData.Clear(ConquerorContextDataFlowDirection flowDirection)
    {
        if (contextData is null)
        {
            return;
        }

        TransportableContextData.Clear(flowDirection);
    }

    IEnumerable<(string Key, string Value)> ITransportableConquerorContextData.GetAll(ConquerorContextDataFlowDirection flowDirection)
        => contextData is null ? [] : TransportableContextData.GetAll(flowDirection);

    string? ITransportableConquerorContextData.Get(string key, ConquerorContextDataFlowDirection flowDirection)
        => contextData is null ? null : TransportableContextData.Get(key, flowDirection);

    bool IInProcessConquerorContextData.Add(string key, object value, ConquerorContextDataFlowDirection flowDirection)
        => InProcessContextData.Add(key, value, flowDirection);

    void IInProcessConquerorContextData.Set(string key, object value, ConquerorContextDataFlowDirection flowDirection)
        => InProcessContextData.Set(key, value, flowDirection);

    bool IInProcessConquerorContextData.Remove(string key, ConquerorContextDataFlowDirection flowDirection)
        => contextData is not null && InProcessContextData.Remove(key, flowDirection);

    void IInProcessConquerorContextData.Clear(ConquerorContextDataFlowDirection flowDirection)
    {
        if (contextData is null)
        {
            return;
        }

        InProcessContextData.Clear(flowDirection);
    }

    IEnumerable<(string Key, object Value)> IInProcessConquerorContextData.GetAll(ConquerorContextDataFlowDirection flowDirection)
        => contextData is null ? [] : InProcessContextData.GetAll(flowDirection);

    T? IInProcessConquerorContextData.Get<T>(string key, ConquerorContextDataFlowDirection flowDirection)
        where T : default
        => contextData is null ? default : InProcessContextData.Get<T>(key, flowDirection);

    #endregion
}
