using System.Diagnostics;

namespace Conqueror.Transport.Http.Tests;

public sealed class HttpTransportTestTimeouts : IDisposable
{
    private static readonly bool IsRunningInGithubActionField = Environment.GetEnvironmentVariable("GITHUB_ACTION") is not null;

    private HttpTransportTestTimeouts()
    {
    }

    public required TimeSpan TestTimeout { get; init; }

    public TimeSpan AssertionTimeout => TimeSpan.FromMilliseconds(AssertionTimeoutInMs);

    public required int AssertionTimeoutInMs { get; init; }

    public required TimeSpan ShortDelay { get; init; }

    public CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

    private CancellationTokenSource TimeoutCancellationTokenSource { get; } = new();

    public bool IsRunningInGithubAction => IsRunningInGithubActionField;

    public static HttpTransportTestTimeouts Create(TimeSpan? testTimeout = null)
    {
        var assertionTimeout = Debugger.IsAttached
            ? TimeSpan.FromMinutes(1)
            : TimeSpan.FromMilliseconds(IsRunningInGithubActionField ? 10_000 : 1_000);

        var testHost = new HttpTransportTestTimeouts
        {
            TestTimeout = testTimeout ?? TimeSpan.FromSeconds(IsRunningInGithubActionField ? 30 : 3),
            AssertionTimeoutInMs = (int)assertionTimeout.TotalMilliseconds,
            ShortDelay = TimeSpan.FromMilliseconds(IsRunningInGithubActionField ? 1_000 : 100),
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
