namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.MultipleIteratorTypes;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[Iterator<TestItem1>]
public partial record TestIterator1;

[Iterator<TestItem2>]
public partial record TestIterator2;

public record TestItem1;

public record TestItem2;

public partial class TestIterator1Handler : TestIterator1.IHandler
{
    public async IAsyncEnumerable<TestItem1> Handle(
        TestIterator1 iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

public partial class TestIterator2Handler : TestIterator2.IHandler
{
    public async IAsyncEnumerable<TestItem2> Handle(
        TestIterator2 iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

// make the compiler happy during design time
public partial record TestIterator1
{
    public partial interface IHandler;
}

public partial record TestIterator2
{
    public partial interface IHandler;
}
