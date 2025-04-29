using System.ComponentModel.DataAnnotations;
using Conqueror;

namespace Quickstart;

// In Conqueror, everything revolves around contracts of different kinds: messages, signals, and
// iterators (the latter is still experimental and therefore not yet included in the Quickstart).
// The contracts are simple records or classes marked by one or more attributes which determine the
// kind and transports (in-process, HTTP, gRPC, RabbitMQ, etc.) of the contract. A source generator
// is used to enhance the contracts with additional code, and therefore they must be partial

// Note that using transports is fully optional, and if you want you can use Conqueror purely
// in-process, similar to libraries like MediatR

// The `HttpMessage` attribute tells Conqueror that this message type can be exposed via HTTP
// (using the corresponding transport package). The attribute allows customizing the HTTP endpoint
// method, path, path prefix, version, API group name, etc. (note that all these are optional with
// sensible defaults, in this case leading to `POST /api/v1/incrementCounterByAmount`)
[HttpMessage<CounterIncrementedResponse>(Version = "v1")]
public sealed partial record IncrementCounterByAmount(string CounterName)
{
    // We use simple data annotation validation as an example, but more powerful validation
    // tools like FluentValidation are also supported. Note that the built-in .NET data annotation
    // validation is only supported for properties, not constructor parameters
    [Range(1, long.MaxValue)]
    public required long IncrementBy { get; init; }
}

public sealed record CounterIncrementedResponse(long NewCounterValue);

// By default, HTTP messages are sent and received as POST, but all methods are supported.
// Parameters can be optional, and messages can have enumerable responses as well
[HttpMessage<List<CounterValue>>(HttpMethod = "GET", Version = "v1")]
public sealed partial record GetCounters(string? Prefix = null);

public sealed record CounterValue(string CounterName, long Value);

// Signals are a pub/sub mechanism, and can be handled in-process (like we do in this quickstart)
// or published via a transport like RabbitMQ (using the corresponding transport package)
[Signal]
public sealed partial record CounterIncremented(
    string CounterName,
    long NewValue,
    long IncrementBy);
