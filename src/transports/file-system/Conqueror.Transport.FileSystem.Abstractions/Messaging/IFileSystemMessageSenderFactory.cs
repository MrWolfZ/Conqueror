// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemMessageSenderFactory
{
    IFileSystemMessageSender<TMessage, TResponse> Create<TMessage, TResponse>(string baseDirectoryPath, TimeSpan pollingInterval)
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>;
}
