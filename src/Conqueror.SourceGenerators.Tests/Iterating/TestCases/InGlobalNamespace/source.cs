using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable CheckNamespace
#pragma warning disable CA1050 // Declare types in namespaces

[Iterator<GlobalTestItem>]
public partial record GlobalTestIterator;

public record GlobalTestItem;

public partial class GlobalTestIteratorHandler : GlobalTestIterator.IHandler
{
    public async IAsyncEnumerable<GlobalTestItem> Handle(
        GlobalTestIterator iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.CompletedTask;
        yield break;
    }
}

// make the compiler happy during design time
public partial record GlobalTestIterator
{
    public partial interface IHandler;
}
