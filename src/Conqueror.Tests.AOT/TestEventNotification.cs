using System.Text.Json.Serialization;

namespace Conqueror.Tests.AOT;

[EventNotification]
public sealed partial record TestEventNotification
{
    public required int Payload { get; init; }
}

internal sealed class TestEventNotificationHandler : TestEventNotification.IHandler
{
    public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"handled notification: {notification}");
        return Task.CompletedTask;
    }

    public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
        where T : class, IEventNotification<T>
        => pipeline.UseLogging();

    public static void ConfigureInProcessReceiver<T>(IInProcessEventNotificationReceiver<T> receiver)
        where T : class, IEventNotification<T>
    {
        receiver.Enable();
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TestEventNotification))]
internal sealed partial class TestEventNotificationJsonSerializerContext : JsonSerializerContext;
