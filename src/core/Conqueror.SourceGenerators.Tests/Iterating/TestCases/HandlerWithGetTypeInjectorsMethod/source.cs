namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.HandlerWithGetTypeInjectorsMethod;

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
    static IEnumerable<IIteratorHandlerTypesInjector> IIteratorHandler.GetTypeInjectors() =>
        throw new NotSupportedException();

    public async IAsyncEnumerable<TestItem> Handle(
        TestIterator iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await default(ValueTask);
        yield break;
    }
}

// make the compiler happy during design time
public partial record TestIterator
{
    public partial interface IHandler;
}

public partial class TestIteratorHandler : IIteratorHandler;
