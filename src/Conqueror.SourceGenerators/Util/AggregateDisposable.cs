namespace Conqueror.SourceGenerators.Util;

internal sealed class AggregateDisposable : IDisposable
{
    private readonly List<IDisposable> disposables = [];

    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    public void Add(IDisposable disposable) => disposables.Insert(index: 0, disposable);
}
