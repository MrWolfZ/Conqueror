using System;
using System.Diagnostics;
using System.Threading;

namespace Conqueror.Middleware.Logging;

internal static class LoggingStopwatch
{
    private static readonly AsyncLocal<Func<TimeSpan>?> TimingFactoryAsyncLocal = new();

    public static RunningLoggingStopwatch StartNew() => new(Stopwatch.StartNew(), TimingFactoryAsyncLocal.Value);

    public static IDisposable WithTimingFactory(Func<TimeSpan> timingFactory)
    {
        TimingFactoryAsyncLocal.Value = timingFactory;

        return new AnonymousDisposable(() => TimingFactoryAsyncLocal.Value = null);
    }

    private sealed class AnonymousDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}

internal sealed class RunningLoggingStopwatch(Stopwatch stopwatch, Func<TimeSpan>? timingFactory)
{
    public TimeSpan Elapsed => timingFactory?.Invoke() ?? stopwatch.Elapsed;
}
