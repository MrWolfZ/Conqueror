using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

// TODO: make this public once the API is more stable
internal interface IFileSystemSignalSerializer<TSignal>
    where TSignal : class, IFileSystemSignal<TSignal>
{
    string FileExtension { get; }

    Task SerializeSignal(
        IServiceProvider serviceProvider,
        TSignal signal,
        Stream fileStream,
        CancellationToken cancellationToken);

    Task<TSignal> DeserializeSignal(
        IServiceProvider serviceProvider,
        Stream fileStream,
        CancellationToken cancellationToken);
}
