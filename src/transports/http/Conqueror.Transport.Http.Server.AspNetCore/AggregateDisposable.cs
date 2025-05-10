using System;
using System.Collections.Generic;

namespace Conqueror.Transport.Http.Server.AspNetCore;

internal sealed class AggregateDisposable : IDisposable
{
    private readonly List<IDisposable> disposables = [];

    public void Add(IDisposable disposable) => disposables.Add(disposable);

    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }
}
