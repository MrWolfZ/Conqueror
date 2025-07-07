// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemSignalPublisherFactory
{
    IFileSystemSignalPublisher<TSignal> Get<TSignal>(string baseDirectoryPath)
        where TSignal : class, IFileSystemSignal<TSignal>;
}
