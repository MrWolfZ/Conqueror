namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.InSameNestedType;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public partial class OuterClass
{
    public void Method()
    {
        // nothing to do
    }

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
}

// make the compiler happy during design time
public partial class OuterClass
{
    public partial record TestIterator
    {
        public partial interface IHandler;
    }
}
