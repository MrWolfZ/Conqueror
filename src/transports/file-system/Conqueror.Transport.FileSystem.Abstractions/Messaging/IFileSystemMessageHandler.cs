using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemMessageHandler
{
    static abstract void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver);
}

public interface IFileSystemMessageHandler<TMessage, TResponse, TIHandler> : IMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
    where TIHandler : class, IFileSystemMessageHandler<TMessage, TResponse, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static IMessageHandlerTypesInjector CreateFileSystemTypesInjector<THandler>()
        where THandler : class, TIHandler, IFileSystemMessageHandler
        => new FileSystemMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(THandler.ConfigureFileSystemReceiver);
}
