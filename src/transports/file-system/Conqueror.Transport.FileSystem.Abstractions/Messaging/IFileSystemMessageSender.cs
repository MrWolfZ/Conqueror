namespace Conqueror;

public interface IFileSystemMessageSender<in TMessage, TResponse> : IMessageSender<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    IFileSystemMessageSender<TMessage, TResponse> WithTimeToLive(TimeSpan timeToLive);
}
