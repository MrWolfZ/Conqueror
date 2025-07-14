using System.Diagnostics;

namespace Conqueror.Transport.FileSystem.Tests;

public sealed class FileSystemTransportTestTimeouts : IDisposable
{
    private static readonly bool IsRunningInGithubActionField = Environment.GetEnvironmentVariable("GITHUB_ACTION") is not null;

    private FileSystemTransportTestTimeouts()
    {
    }

    public required TimeSpan TestTimeout { get; init; }

    public TimeSpan AssertionTimeout => TimeSpan.FromMilliseconds(AssertionTimeoutInMs);

    public required int AssertionTimeoutInMs { get; init; }

    public required TimeSpan ShortDelay { get; init; }

    public CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

    private CancellationTokenSource TimeoutCancellationTokenSource { get; } = new();

    public static FileSystemTransportTestTimeouts Create(TimeSpan? testTimeout = null)
    {
        var assertionTimeout = Debugger.IsAttached
            ? TimeSpan.FromMinutes(1)
            : TimeSpan.FromMilliseconds(IsRunningInGithubActionField ? 10_000 : 3_000);

        var testHost = new FileSystemTransportTestTimeouts
        {
            TestTimeout = testTimeout ?? TimeSpan.FromSeconds(IsRunningInGithubActionField ? 30 : 5),
            AssertionTimeoutInMs = (int)assertionTimeout.TotalMilliseconds,
            ShortDelay = TimeSpan.FromMilliseconds(IsRunningInGithubActionField ? 2_000 : 200),
        };

        if (!Debugger.IsAttached)
        {
            testHost.TimeoutCancellationTokenSource.CancelAfter(testHost.TestTimeout);
        }

        return testHost;
    }

    public void Dispose()
    {
        TimeoutCancellationTokenSource.Cancel();

        TimeoutCancellationTokenSource.Dispose();
    }
}
