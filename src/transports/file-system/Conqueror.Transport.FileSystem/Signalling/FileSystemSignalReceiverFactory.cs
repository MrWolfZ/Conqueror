namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalReceiverFactory(IServiceProvider serviceProvider)
    : ISignalReceiverFactory<IFileSystemSignalHandlerTypesInjector, FileSystemSignalReceiver>
{
    private static readonly ConfigurationInjectable ConfigInjectable = new();

    public string TransportTypeName => TransportName;

    public FileSystemSignalReceiver? CreateReceiverForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<ISignalReceiverHandlerInvoker<IFileSystemSignalHandlerTypesInjector>> invokers,
        IFileSystemSignalHandlerTypesInjector typesInjector)
    {
        var receiver = new FileSystemSignalReceiver(serviceProvider, invokers.Select(i => i.SignalType).ToArray(), handlerType);
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

    private readonly record struct ConfigurationInjectableArg(ISignalReceiverHandlerInvoker Invoker, FileSystemSignalReceiver Receiver);

    private sealed class ConfigurationInjectable : IFileSystemSignalTypesInjectable<ConfigurationInjectableArg, object?>
    {
        object? IFileSystemSignalTypesInjectable<ConfigurationInjectableArg, object?>
            .WithInjectedTypes<TSignal, TIHandler>(ConfigurationInjectableArg arg)
        {
            arg.Receiver.AddSignalType<TSignal>(arg.Invoker);

            return null;
        }
    }
}
