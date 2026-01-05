namespace Conqueror.SourceGenerators.Tests.Iterating.TestCases.HandlerWithModuleInitializerMethod;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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

    [ModuleInitializer]
    public static void ModuleInitializer()
    {
        // Method intentionally left empty.
    }
}

// make the compiler happy during design time
public partial record TestIterator
{
    public partial interface IHandler;
}

public partial class TestIteratorHandler : IIteratorHandler;
