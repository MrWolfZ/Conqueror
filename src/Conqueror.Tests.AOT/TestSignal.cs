using System.Text.Json.Serialization;

namespace Conqueror.Tests.AOT;

[Signal]
public sealed partial record TestSignal
{
    public required int Payload { get; init; }
}

internal sealed partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"handled signal: {signal}");
        return Task.CompletedTask;
    }

    public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
        where T : class, ISignal<T>
        => pipeline.UseLogging();

    public static void ConfigureInProcessReceiver<T>(IInProcessSignalReceiver<T> receiver)
        where T : class, ISignal<T>
    {
        receiver.Enable();
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TestSignal))]
internal sealed partial class TestSignalJsonSerializerContext : JsonSerializerContext;
