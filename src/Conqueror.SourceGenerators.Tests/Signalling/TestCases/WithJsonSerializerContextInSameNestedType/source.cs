using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithJsonSerializerContextInSameNestedType;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Signal]
    public partial record TestSignal;

    public record TestSignalResponse;

    public partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal class TestSignalJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
    {
        protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

        public static JsonSerializerContext Default => null!;

        public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    public partial record TestSignal
    {
        public partial interface IHandler;
    }
}
