namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.Basic;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[Iterator<TestItem>]
public partial record TestIterator;

public record TestItem;

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

// make the compiler happy during design time
public partial record TestIterator
{
    public partial interface IHandler;
}
