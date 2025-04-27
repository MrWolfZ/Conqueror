using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithJsonSerializerContextInSameNamespace;

[Signal]
public sealed partial record TestSignal;

public sealed record TestSignalResponse;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

[JsonSerializable(typeof(TestSignal))]
[JsonSerializable(typeof(TestSignalResponse))]
internal class TestSignalJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
{
    public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();

    protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

    public static JsonSerializerContext Default => null!;
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}
