namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageReceiverFactory(IServiceProvider serviceProvider)
    : IMessageReceiverFactory<IFileSystemMessageHandlerTypesInjector, FileSystemMessageReceiver>
{
    private static readonly ConfigurationInjectable ConfigInjectable = new();

    public string TransportTypeName => TransportName;

    public FileSystemMessageReceiver? CreateReceiverForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<IMessageReceiverHandlerInvoker<IFileSystemMessageHandlerTypesInjector>> invokers,
        IFileSystemMessageHandlerTypesInjector typesInjector)
    {
        var receiver = new FileSystemMessageReceiver(serviceProvider, invokers.Select(i => i.MessageType).ToArray(), handlerType);
        typesInjector.ConfigureFileSystemReceiver(receiver);

        if (!receiver.IsEnabled)
        {
            return null;
        }

        foreach (var invoker in invokers)
        {
            _ = invoker.TypesInjector.Inject(ConfigInjectable, new(invoker, receiver));
        }

        return receiver;
    }

    private readonly record struct ConfigurationInjectableArg(IMessageReceiverHandlerInvoker Invoker, FileSystemMessageReceiver Receiver);

    private sealed class ConfigurationInjectable : IFileSystemMessageTypesInjectable<ConfigurationInjectableArg, object?>
    {
        object? IFileSystemMessageTypesInjectable<ConfigurationInjectableArg, object?>
            .WithInjectedTypes<TMessage, TResponse, TIHandler>(ConfigurationInjectableArg arg)
        {
            arg.Receiver.AddMessageType<TMessage, TResponse>(arg.Invoker);

            return null;
        }
    }
}
