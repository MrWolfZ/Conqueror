namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.GenericWithHierarchy;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[Iterator<TestItem>]
public abstract partial record TestIterator<TPayload>;

[Iterator<TestItem>]
public partial record TestIteratorSub<TPayload> : TestIterator<TPayload>;

public record TestItem;

public partial class TestIteratorHandler<TPayload> : TestIterator<TPayload>.IHandler
{
    public async IAsyncEnumerable<TestItem> Handle(
        TestIterator<TPayload> iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

public partial class TestIteratorSubHandler<TPayload> : TestIteratorSub<TPayload>.IHandler
{
    public async IAsyncEnumerable<TestItem> Handle(
        TestIteratorSub<TPayload> iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

// make the compiler happy during design time
public partial record TestIterator<TPayload>
{
    public partial interface IHandler;
}

public partial record TestIteratorSub<TPayload>
{
    public new partial interface IHandler;
}
