namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.ItemTypeInDifferentNamespace
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using DifferentNamespace.For.Item;

    [Iterator<TestItem>]
    public partial record TestIterator;

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

namespace DifferentNamespace.For.Item
{
    public record TestItem;
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.ItemTypeInDifferentNamespace
{
    public partial record TestIterator
    {
        public partial interface IHandler;
    }
}
