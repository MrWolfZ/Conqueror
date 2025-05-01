namespace Examples.BlazorWebAssembly.API;

public static class SystemTime
{
    private static readonly AsyncLocal<DateTimeOffset?> CurrentTimeAsyncLocal = new();

    public static DateTimeOffset Now => CurrentTimeAsyncLocal.Value ?? DateTimeOffset.UtcNow;

    public static IDisposable WithCurrentTime(DateTimeOffset time)
    {
        CurrentTimeAsyncLocal.Value = time;

        return new AnonymousDisposable(() => CurrentTimeAsyncLocal.Value = null);
    }

    private sealed class AnonymousDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
