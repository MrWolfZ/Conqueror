namespace Conqueror;

public interface IFileSystemSignalHandler
{
    static abstract void ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver);
}

public interface IFileSystemSignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>
    where TSignal : class, IFileSystemSignal<TSignal>
    where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateFileSystemTypesInjector<THandler>()
        where THandler : class, TIHandler, IFileSystemSignalHandler =>
        new FileSystemSignalHandlerTypesInjector<TSignal, TIHandler>(THandler.ConfigureFileSystemReceiver);
}
