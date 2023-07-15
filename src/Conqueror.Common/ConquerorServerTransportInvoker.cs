using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Conqueror.Common;

public abstract class ConquerorServerTransportInvoker
{
    private readonly IConquerorContextAccessor conquerorContextAccessor;

    protected ConquerorServerTransportInvoker(IConquerorContextAccessor conquerorContextAccessor)
    {
        this.conquerorContextAccessor = conquerorContextAccessor;
    }

    public async Task Execute(Func<Task> executeFn)
    {
        using var conquerorContext = conquerorContextAccessor.GetOrCreate();

        var parsedDownstreamData = ConquerorContextDataFormatter.Parse(GetFormattedDownstreamContextData());

        foreach (var (key, value) in parsedDownstreamData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var parsedData = ConquerorContextDataFormatter.Parse(GetFormattedContextData());

        foreach (var (key, value) in parsedData)
        {
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        conquerorContext.SignalExecutionFromTransport();

        if (Activity.Current is null && GetTraceParent() is { } traceParent)
        {
            using var a = new Activity(string.Empty);
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.SetTraceId(traceId);
        }

        await executeFn().ConfigureAwait(false);

        if (ConquerorContextDataFormatter.Format(conquerorContext.UpstreamContextData) is { } formattedUpstreamData)
        {
            SetFormattedUpstreamContextData(formattedUpstreamData);
        }

        if (ConquerorContextDataFormatter.Format(conquerorContext.ContextData) is { } formattedData)
        {
            SetFormattedContextData(formattedData);
        }
    }

    protected abstract IEnumerable<string> GetFormattedDownstreamContextData();

    protected abstract IEnumerable<string> GetFormattedContextData();

    protected abstract string? GetTraceParent();

    protected abstract void SetFormattedUpstreamContextData(string formattedData);

    protected abstract void SetFormattedContextData(string formattedData);
}
