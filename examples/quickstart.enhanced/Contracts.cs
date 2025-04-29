using System.ComponentModel.DataAnnotations;
using Conqueror;

namespace Quickstart.Enhanced;

[HttpMessage<CounterIncrementedResponse>(Version = "v1")]
public sealed partial record IncrementCounterByAmount(string CounterName)
{
    [Range(1, long.MaxValue)]
    public required long IncrementBy { get; init; }
}

public sealed record CounterIncrementedResponse(long NewCounterValue);

[HttpMessage<List<CounterValue>>(HttpMethod = "GET", Version = "v1")]
public sealed partial record GetCounters(string? Prefix = null);

public sealed record CounterValue(string CounterName, long Value);

[Signal]
public sealed partial record CounterIncremented(
    string CounterName,
    long NewValue,
    long IncrementBy);
