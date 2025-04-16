using System;

namespace Conqueror.SourceGenerators.Util;

internal sealed class AnonymousDisposable(Action onDispose) : IDisposable
{
    public void Dispose() => onDispose();
}
