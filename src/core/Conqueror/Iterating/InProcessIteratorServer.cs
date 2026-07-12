namespace Conqueror.Iterating;

internal sealed class InProcessIteratorServer<TIterator, TItem>(IServiceProvider serviceProvider)
    : IInProcessIteratorServer
    where TIterator : class, IIterator<TIterator, TItem>
{
    public bool MustBeConfiguredOnEveryIteration { get; private set; }
    public Type IteratorType { get; } = typeof(TIterator);

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public bool IsEnabled { get; private set; } = true;

    public void Disable() => IsEnabled = false;

    public void ConfigureOnEveryIteration()
    {
        MustBeConfiguredOnEveryIteration = true;
    }
}
