// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalPublisher<in TSignal> : ISignalPublisher<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>;
