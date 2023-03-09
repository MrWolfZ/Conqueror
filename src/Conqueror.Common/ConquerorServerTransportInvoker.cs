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

        var parsedValue = ConquerorContextDataFormatter.Parse(GetFormattedDownstreamContextData());

        foreach (var (key, value) in parsedValue)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        conquerorContext.SignalExecutionFromTransport();

        if (Activity.Current is null && GetTraceParent() is { } traceParent)
        {
            using var a = new Activity(string.Empty);
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.SetTraceId(traceId);
        }

        await executeFn().ConfigureAwait(false);

        if (ConquerorContextDataFormatter.Format(conquerorContext.UpstreamContextData) is { } s)
        {
            SetFormattedUpstreamContextData(s);
        }
    }

    protected abstract IEnumerable<string> GetFormattedDownstreamContextData();

    protected abstract string? GetTraceParent();

    protected abstract void SetFormattedUpstreamContextData(string formattedData);
}
