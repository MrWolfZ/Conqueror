namespace Conqueror.Context;

internal sealed class DefaultConquerorContextData(DefaultConquerorContextData? parent = null)
    : ITransportableConquerorContextData,
        IInProcessConquerorContextData
{
    private DefaultInProcessConquerorContextData? inProcessBidirectionalContextData =
        parent?.inProcessBidirectionalContextData is not null ? new(parent.inProcessBidirectionalContextData) : null;

    private DefaultInProcessConquerorContextData? inProcessDownstreamContextData =
        parent?.inProcessDownstreamContextData is not null ? new(parent.inProcessDownstreamContextData) : null;

    private DefaultInProcessConquerorContextData? inProcessUpstreamContextData;

    private DefaultTransportableConquerorContextData? transportableBidirectionalContextData =
        parent?.transportableBidirectionalContextData is not null
            ? new(parent.transportableBidirectionalContextData)
            : null;

    private DefaultTransportableConquerorContextData? transportableDownstreamContextData =
        parent?.transportableDownstreamContextData is not null ? new(parent.transportableDownstreamContextData) : null;

    private DefaultTransportableConquerorContextData? transportableUpstreamContextData;

    private DefaultTransportableConquerorContextData TransportableDownstreamContextData =>
        LazyInitializer.EnsureInitialized(ref transportableDownstreamContextData, static () => []);

    private DefaultTransportableConquerorContextData TransportableUpstreamContextData =>
        LazyInitializer.EnsureInitialized(ref transportableUpstreamContextData, static () => []);

    private DefaultTransportableConquerorContextData TransportableBidirectionalContextData =>
        LazyInitializer.EnsureInitialized(ref transportableBidirectionalContextData, static () => []);

    private DefaultInProcessConquerorContextData InProcessDownstreamContextData =>
        LazyInitializer.EnsureInitialized(ref inProcessDownstreamContextData, static () => []);

    private DefaultInProcessConquerorContextData InProcessUpstreamContextData =>
        LazyInitializer.EnsureInitialized(ref inProcessUpstreamContextData, static () => []);

    private DefaultInProcessConquerorContextData InProcessBidirectionalContextData =>
        LazyInitializer.EnsureInitialized(ref inProcessBidirectionalContextData, static () => []);

    bool IInProcessConquerorContextData.Add(string key, object value, ConquerorContextDataFlowDirection flowDirection)
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => InProcessDownstreamContextData.Add(key, value),
            ConquerorContextDataFlowDirection.Upstream => InProcessUpstreamContextData.Add(key, value),
            ConquerorContextDataFlowDirection.Bidirectional => InProcessBidirectionalContextData.Add(key, value),
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    void IInProcessConquerorContextData.Set(string key, object value, ConquerorContextDataFlowDirection flowDirection)
    {
        switch (flowDirection)
        {
            case ConquerorContextDataFlowDirection.Downstream:
                InProcessDownstreamContextData.Set(key, value);

                break;

            case ConquerorContextDataFlowDirection.Upstream:
                InProcessUpstreamContextData.Set(key, value);

                break;

            case ConquerorContextDataFlowDirection.Bidirectional:
                InProcessBidirectionalContextData.Set(key, value);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null);
        }
    }

    bool IInProcessConquerorContextData.Remove(string key, ConquerorContextDataFlowDirection flowDirection)
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => InProcessDownstreamContextData.Remove(key),
            ConquerorContextDataFlowDirection.Upstream => InProcessUpstreamContextData.Remove(key),
            ConquerorContextDataFlowDirection.Bidirectional => InProcessBidirectionalContextData.Remove(key),
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    void IInProcessConquerorContextData.Clear(ConquerorContextDataFlowDirection flowDirection)
    {
        switch (flowDirection)
        {
            case ConquerorContextDataFlowDirection.Downstream:
                InProcessDownstreamContextData.Clear();

                break;

            case ConquerorContextDataFlowDirection.Upstream:
                InProcessUpstreamContextData.Clear();

                break;

            case ConquerorContextDataFlowDirection.Bidirectional:
                InProcessBidirectionalContextData.Clear();

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null);
        }
    }

    IEnumerable<(string Key, object Value)> IInProcessConquerorContextData.GetAll(
        ConquerorContextDataFlowDirection flowDirection
    )
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => inProcessDownstreamContextData
                ?? DefaultInProcessConquerorContextData.Empty,
            ConquerorContextDataFlowDirection.Upstream => inProcessUpstreamContextData
                ?? DefaultInProcessConquerorContextData.Empty,
            ConquerorContextDataFlowDirection.Bidirectional => inProcessBidirectionalContextData
                ?? DefaultInProcessConquerorContextData.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    T? IInProcessConquerorContextData.Get<T>(string key, ConquerorContextDataFlowDirection flowDirection)
        where T : default
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => InProcessDownstreamContextData.Get<T>(key),
            ConquerorContextDataFlowDirection.Upstream => InProcessUpstreamContextData.Get<T>(key),
            ConquerorContextDataFlowDirection.Bidirectional => InProcessBidirectionalContextData.Get<T>(key),
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    bool ITransportableConquerorContextData.Add(
        string key,
        string value,
        ConquerorContextDataFlowDirection flowDirection
    )
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => TransportableDownstreamContextData.Add(key, value),
            ConquerorContextDataFlowDirection.Upstream => TransportableUpstreamContextData.Add(key, value),
            ConquerorContextDataFlowDirection.Bidirectional => TransportableBidirectionalContextData.Add(key, value),
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    void ITransportableConquerorContextData.Set(
        string key,
        string value,
        ConquerorContextDataFlowDirection flowDirection
    )
    {
        switch (flowDirection)
        {
            case ConquerorContextDataFlowDirection.Downstream:
                TransportableDownstreamContextData.Set(key, value);

                break;

            case ConquerorContextDataFlowDirection.Upstream:
                TransportableUpstreamContextData.Set(key, value);

                break;

            case ConquerorContextDataFlowDirection.Bidirectional:
                TransportableBidirectionalContextData.Set(key, value);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null);
        }
    }

    bool ITransportableConquerorContextData.Remove(string key, ConquerorContextDataFlowDirection flowDirection)
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => TransportableDownstreamContextData.Remove(key),
            ConquerorContextDataFlowDirection.Upstream => TransportableUpstreamContextData.Remove(key),
            ConquerorContextDataFlowDirection.Bidirectional => TransportableBidirectionalContextData.Remove(key),
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    void ITransportableConquerorContextData.Clear(ConquerorContextDataFlowDirection flowDirection)
    {
        switch (flowDirection)
        {
            case ConquerorContextDataFlowDirection.Downstream:
                TransportableDownstreamContextData.Clear();

                break;

            case ConquerorContextDataFlowDirection.Upstream:
                TransportableUpstreamContextData.Clear();

                break;

            case ConquerorContextDataFlowDirection.Bidirectional:
                TransportableBidirectionalContextData.Clear();

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null);
        }
    }

    IEnumerable<(string Key, string Value)> ITransportableConquerorContextData.GetAll(
        ConquerorContextDataFlowDirection flowDirection
    )
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => transportableDownstreamContextData
                ?? DefaultTransportableConquerorContextData.Empty,
            ConquerorContextDataFlowDirection.Upstream => transportableUpstreamContextData
                ?? DefaultTransportableConquerorContextData.Empty,
            ConquerorContextDataFlowDirection.Bidirectional => transportableBidirectionalContextData
                ?? DefaultTransportableConquerorContextData.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    string? ITransportableConquerorContextData.Get(string key, ConquerorContextDataFlowDirection flowDirection)
    {
        return flowDirection switch
        {
            ConquerorContextDataFlowDirection.Downstream => TransportableDownstreamContextData.Get(key),
            ConquerorContextDataFlowDirection.Upstream => TransportableUpstreamContextData.Get(key),
            ConquerorContextDataFlowDirection.Bidirectional => TransportableBidirectionalContextData.Get(key),
            _ => throw new ArgumentOutOfRangeException(nameof(flowDirection), flowDirection, message: null),
        };
    }

    public void PropagateUpstreamData(DefaultConquerorContextData childData)
    {
        if (childData.transportableUpstreamContextData is not null)
        {
            foreach (var (key, value) in childData.transportableUpstreamContextData)
            {
                TransportableUpstreamContextData.Set(key, value);
            }
        }

        if (childData.inProcessUpstreamContextData is not null)
        {
            foreach (var (key, value) in childData.inProcessUpstreamContextData)
            {
                InProcessUpstreamContextData.Set(key, value);
            }
        }

        if (transportableBidirectionalContextData is not null)
        {
            // bidirectional keys also propagate deletion upstream
            foreach (var (key, _) in transportableBidirectionalContextData)
            {
                if (childData.transportableBidirectionalContextData?.IsRemoved(key) ?? false)
                {
                    _ = transportableBidirectionalContextData.Remove(key);
                }
            }
        }

        if (inProcessBidirectionalContextData is not null)
        {
            // bidirectional keys also propagate deletion upstream
            foreach (var (key, _) in inProcessBidirectionalContextData)
            {
                if (childData.inProcessBidirectionalContextData?.IsRemoved(key) ?? false)
                {
                    _ = inProcessBidirectionalContextData.Remove(key);
                }
            }
        }

        if (childData.transportableBidirectionalContextData is not null)
        {
            foreach (var (key, value) in childData.transportableBidirectionalContextData)
            {
                TransportableBidirectionalContextData.Set(key, value);
            }
        }

        if (childData.inProcessBidirectionalContextData is not null)
        {
            foreach (var (key, value) in childData.inProcessBidirectionalContextData)
            {
                InProcessBidirectionalContextData.Set(key, value);
            }
        }
    }
}
