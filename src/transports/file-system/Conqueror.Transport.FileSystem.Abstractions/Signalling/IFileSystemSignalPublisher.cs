// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemSignalPublisher<in TSignal> : ISignalPublisher<TSignal>
    where TSignal : class, IFileSystemSignal<TSignal>;
