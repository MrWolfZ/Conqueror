namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageReceiver(
    IServiceProvider serviceProvider,
    IReadOnlyCollection<Type> messageTypes,
    Type? handlerType) : IFileSystemMessageReceiver
{
    private readonly Dictionary<string, (string MessageFileExtension, string ResponseFileExtension)> fileExtensionsByTag = [];
    private readonly Dictionary<string, Func<object, CancellationToken, Task<object>>> invokerByTag = [];
    private readonly Dictionary<string, Type> messageTypeByTag = [];
    private readonly Dictionary<string, Func<Stream, CancellationToken, Task<object?>>> parserByTag = [];
    private readonly Dictionary<string, Func<object, Stream, CancellationToken, Task>> responseWriterByTag = [];
    private readonly List<string> tags = [];

    public IReadOnlyCollection<Type> MessageTypes { get; } = messageTypes;

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public bool IsEnabled => Configuration is not null;

    public IReadOnlyCollection<string> Tags => tags;

    public FileSystemMessageReceiverConfiguration? Configuration { get; private set; }

    public FileSystemMessageReceiverConfiguration EnableSingleInstance(string baseDirectoryPath, TimeSpan pollingInterval)
    {
        Configuration = new()
        {
            Name = HandlerType?.Name ?? $"delegate-{string.Join("-", Tags)}",
            BaseDirectoryPath = baseDirectoryPath,

            // by setting the lease duration to zero, messages will be immediately available for reprocessing
            // by the single handler in case of a crash, ensuring in-order delivery
            LeaseDuration = TimeSpan.Zero,
            PollingInterval = pollingInterval,
        };

        return Configuration;
    }

    public FileSystemMessageReceiverConfiguration EnableMultipleCompetingInstances(string baseDirectoryPath, TimeSpan leaseDuration, TimeSpan pollingInterval)
    {
        Configuration = new()
        {
            Name = HandlerType?.Name ?? $"delegate-{string.Join("-", Tags)}",
            BaseDirectoryPath = baseDirectoryPath,
            LeaseDuration = leaseDuration,
            PollingInterval = pollingInterval,
        };

        return Configuration;
    }

    public void Disable()
    {
        Configuration = null;
    }

    public void AddMessageType<TMessage, TResponse>(IMessageReceiverHandlerInvoker invoker)
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
    {
        if (!messageTypeByTag.TryAdd(TMessage.Tag, typeof(TMessage)))
        {
            throw new InvalidOperationException(
                $"the tag '{TMessage.Tag}' is already used by message type '{messageTypeByTag[TMessage.Tag]}'");
        }

        tags.Add(TMessage.Tag);
        tags.Sort(StringComparer.OrdinalIgnoreCase);

        invokerByTag[TMessage.Tag] = async (message, ct)
            =>
        {
            var response = await invoker.Invoke<TMessage, TResponse>(
                                            (TMessage)message,
                                            ServiceProvider,
                                            TransportName,
                                            ct)
                                        .ConfigureAwait(false);

            return response!;
        };

        parserByTag[TMessage.Tag] = async (content, ct)
            => await TMessage.FileSystemMessageSerializer.DeserializeMessage(ServiceProvider, content, ct).ConfigureAwait(false);

        responseWriterByTag[TMessage.Tag] = async (response, content, ct)
            => await TMessage.FileSystemMessageResponseSerializer.SerializeResponse(
                                 ServiceProvider,
                                 (TResponse)response,
                                 content,
                                 ct)
                             .ConfigureAwait(false);

        fileExtensionsByTag[TMessage.Tag] = (
            TMessage.FileSystemMessageSerializer.FileExtension,
            TMessage.FileSystemMessageResponseSerializer.FileExtension);
    }

    public (string MessageFileExtension, string ResponseFileExtension) GetFileExtensions(string tag)
    {
        return fileExtensionsByTag[tag];
    }

    public Task<object?> ReadMessage(string tag, Stream stream, CancellationToken cancellationToken)
    {
        return parserByTag[tag].Invoke(stream, cancellationToken);
    }

    public Task WriteResponse(
        string tag,
        object response,
        Stream stream,
        CancellationToken cancellationToken)
    {
        return responseWriterByTag[tag].Invoke(response, stream, cancellationToken);
    }

    public Task<object> InvokeHandler(string tag, object message, CancellationToken cancellationToken)
    {
        return invokerByTag[tag].Invoke(message, cancellationToken);
    }
}
