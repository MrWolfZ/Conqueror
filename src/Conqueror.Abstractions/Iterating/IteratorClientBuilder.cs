namespace Conqueror;

public readonly record struct IteratorClientBuilder<TIterator, TItem>(IServiceProvider ServiceProvider)
    where TIterator : class, IIterator<TIterator, TItem>;
