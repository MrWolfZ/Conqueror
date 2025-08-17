namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithJsonSerializerContextInDifferentNamespace
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [Signal]
    public sealed partial record TestSignal;

    public sealed record TestSignalResponse;

    public partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace Some.Other.NamespaceWithJsonSerializerContext
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithJsonSerializerContextInDifferentNamespace;

    [JsonSerializable(typeof(TestSignal))]
    [JsonSerializable(typeof(TestSignalResponse))]
    internal class TestSignalJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
    {
        public static JsonSerializerContext Default => null!;

        protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

        public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithJsonSerializerContextInDifferentNamespace
{
    public partial record TestSignal
    {
        public partial interface IHandler;
    }
}
