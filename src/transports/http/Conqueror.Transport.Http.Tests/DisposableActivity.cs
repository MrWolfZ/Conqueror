using System.Diagnostics;

namespace Conqueror.Transport.Http.Tests;

public sealed class DisposableActivity(Activity activity, params IDisposable[] disposables) : IDisposable
{
    private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

    public Activity Activity { get; } = activity;

    public string TraceId => Activity.TraceId.ToString();

    public void Dispose()
    {
        foreach (var disposable in disposables.Reverse())
        {
            disposable.Dispose();
        }
    }

    public static DisposableActivity Create(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        var a = activitySource.CreateActivity(name, ActivityKind.Server)!;
        return new(a, activitySource, activityListener, a);
    }
}
