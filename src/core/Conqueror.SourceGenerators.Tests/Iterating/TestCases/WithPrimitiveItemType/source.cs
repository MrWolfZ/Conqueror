namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.WithPrimitiveItemType;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[Iterator<string>]
public partial record TestIterator;

public partial class TestIteratorHandler : TestIterator.IHandler
{
    public async IAsyncEnumerable<string> Handle(
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
