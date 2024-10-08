namespace Conqueror.Examples.BlazorWebAssembly.Contracts;

public sealed record SharedCounterIncrementedEvent(long NewValue, long IncrementedBy);

public interface ISharedCounterIncrementedEventObserver : IEventObserver<SharedCounterIncrementedEvent>;
