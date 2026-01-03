namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.InPrivateNestedType;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    private sealed partial class PrivateClass
    {
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
                await System.Threading.Tasks.Task.CompletedTask;
                yield break;
            }
        }
    }
}

// make the compiler happy during design time
public partial class Container
{
    private sealed partial class PrivateClass
    {
        public partial record TestIterator
        {
            public partial interface IHandler;
        }
    }
}
