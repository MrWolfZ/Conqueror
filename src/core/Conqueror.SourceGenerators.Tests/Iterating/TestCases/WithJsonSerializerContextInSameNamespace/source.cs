namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithJsonSerializerContextInSameNamespace;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

[Iterator<TestItem>]
public partial record TestIterator;

public sealed record TestItem;

public partial class TestIteratorHandler : TestIterator.IHandler
{
    public async IAsyncEnumerable<TestItem> Handle(
        TestIterator iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

[JsonSerializable(typeof(TestIterator))]
[JsonSerializable(typeof(TestItem))]
internal class TestIteratorJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
{
    public static JsonSerializerContext Default => null!;

    protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

    public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestIterator
{
    public partial interface IHandler;
}
