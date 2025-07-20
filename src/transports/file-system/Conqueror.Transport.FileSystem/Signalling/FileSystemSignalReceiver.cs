namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalReceiver(
    IServiceProvider serviceProvider,
    IReadOnlyCollection<Type> signalTypes,
    Type? handlerType) : IFileSystemSignalReceiver
{
    private readonly Dictionary<string, string> fileExtensionByTag = [];
    private readonly List<ISignalReceiverHandlerInvoker> invokers = [];
    private readonly ConcurrentDictionary<Type, List<ISignalReceiverHandlerInvoker>> invokersBySignalType = [];
    private readonly Dictionary<string, Func<Stream, CancellationToken, Task<object?>>> parserByTag = [];
    private readonly Dictionary<string, Type> signalTypeByTag = [];
    private readonly List<string> tags = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IReadOnlyCollection<Type> SignalTypes { get; } = signalTypes;
    public Type? HandlerType { get; } = handlerType;

    public bool IsEnabled => Configuration is not null;

    public IReadOnlyCollection<string> Tags => tags;

    public FileSystemSignalReceiverConfiguration? Configuration { get; private set; }

    public FileSystemSignalReceiverConfiguration EnableMultipleCompetingInstances(
        string baseDirectoryPath,
        TimeSpan leaseDuration,
        TimeSpan pollingInterval)
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

    public FileSystemSignalReceiverConfiguration EnableSingleInstance(string baseDirectoryPath, TimeSpan pollingInterval)
    {
        Configuration = new()
        {
            Name = HandlerType?.Name ?? $"delegate-{string.Join("-", Tags)}",
            BaseDirectoryPath = baseDirectoryPath,
            LeaseDuration = null,
            PollingInterval = pollingInterval,
        };

        return Configuration;
    }

    public void Disable() => Configuration = null;

    public void AddSignalType<TSignal>(ISignalReceiverHandlerInvoker invoker)
        where TSignal : class, IFileSystemSignal<TSignal>
    {
        if (!signalTypeByTag.TryAdd(TSignal.Tag, typeof(TSignal)))
        {
            throw new InvalidOperationException(
                $"the tag '{TSignal.Tag}' is already used by signal type '{signalTypeByTag[TSignal.Tag]}'");
        }

        tags.Add(TSignal.Tag);
        tags.Sort(StringComparer.OrdinalIgnoreCase);

        invokers.Add(invoker);
        parserByTag[TSignal.Tag] = async (content, ct)
            => await TSignal.FileSystemSignalSerializer.DeserializeSignal(ServiceProvider, content, ct).ConfigureAwait(false);

        fileExtensionByTag[TSignal.Tag] = TSignal.FileSystemSignalSerializer.FileExtension;
    }

    public string GetFileExtension(string tag) => fileExtensionByTag[tag];

    public Task<object?> ReadSignal(string tag, Stream stream, CancellationToken cancellationToken)
    {
        return parserByTag[tag].Invoke(stream, cancellationToken);
    }

    public async Task InvokeHandler(object signal, CancellationToken cancellationToken)
    {
        var relevantInvokers = invokersBySignalType.GetOrAdd(signal.GetType(), _ => invokers.Where(i => i.SignalType.IsInstanceOfType(signal)).ToList());

        // looping over the invokers handles the edge case where a handler observes a signal
        // multiple times through the signal's type hierarchy
        foreach (var invoker in relevantInvokers)
        {
            await invoker.Invoke(
                             signal,
                             ServiceProvider,
                             TransportName,
                             cancellationToken)
                         .ConfigureAwait(false);
        }
    }
}
