namespace Conqueror.Transport.FileSystem.Tests;

public sealed class FileSystemTransportTestHost
{
    private static readonly bool IsRunningInGithubActionField =
        Environment.GetEnvironmentVariable("GITHUB_ACTION") is not null;

    private FileSystemTransportTestHost() { }

    public required IHost Host { get; init; }

    public bool IsRunningInGithubAction => IsRunningInGithubActionField;

    public static async Task<FileSystemTransportTestHost> Create(
        Action<IServiceCollection> configureServices,
        CancellationToken cancellationToken
    )
    {
        var hostBuilder = new HostBuilder()
            .ConfigureLogging(logging => logging.AddTestLogger().SetMinimumLevel(LogLevel.Trace))
            .UseEnvironment(Environments.Development)
            .ConfigureServices(services =>
            {
                configureServices?.Invoke(services);
            });

        var host = await hostBuilder.StartAsync(cancellationToken);

        var testHost = new FileSystemTransportTestHost { Host = host };

        return testHost;
    }

    public T Resolve<T>()
        where T : notnull => Host.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(Host);

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}
