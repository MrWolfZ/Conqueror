namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.Generic;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[Iterator<TestItem>]
public partial record TestIterator<TFirst, TSecond>;

public record TestItem;

public partial class TestIteratorHandler<TFirst, TSecond> : TestIterator<TFirst, TSecond>.IHandler
{
    public async IAsyncEnumerable<TestItem> Handle(
        TestIterator<TFirst, TSecond> iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

public partial class TestIteratorHandler2 : TestIterator<string, int>.IHandler
{
    public async IAsyncEnumerable<TestItem> Handle(
        TestIterator<string, int> iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

// make the compiler happy during design time
public partial record TestIterator<TFirst, TSecond>
{
    public partial interface IHandler;
}
