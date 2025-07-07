using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IFileSystemMessageSerializer<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    string FileExtension { get; }

    Task SerializeMessage(
        IServiceProvider serviceProvider,
        TMessage message,
        Stream fileStream,
        CancellationToken cancellationToken);

    Task<TMessage> DeserializeMessage(
        IServiceProvider serviceProvider,
        Stream fileStream,
        CancellationToken cancellationToken);
}

internal interface IFileSystemMessageResponseSerializer<TMessage, TResponse>
    where TMessage : class, IFileSystemMessage<TMessage, TResponse>
{
    string FileExtension { get; }

    Task SerializeResponse(
        IServiceProvider serviceProvider,
        TResponse response,
        Stream fileStream,
        CancellationToken cancellationToken);

    Task<TResponse> DeserializeResponse(
        IServiceProvider serviceProvider,
        Stream fileStream,
        CancellationToken cancellationToken);
}
