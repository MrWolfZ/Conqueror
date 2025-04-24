using System;
using System.Collections.Generic;

namespace Conqueror.SourceGenerators.Util;

internal sealed class AggregateDisposable : IDisposable
{
    private readonly List<IDisposable> disposables = [];

    public void Add(IDisposable disposable) => disposables.Insert(0, disposable);

    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }
}
