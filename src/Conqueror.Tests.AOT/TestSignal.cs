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

    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
        => pipeline.UseLogging();

    static void ISignalHandler.ConfigureInProcessReceiver<T>(IInProcessSignalReceiver<T> receiver)
    {
        receiver.Enable();
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TestSignal))]
internal sealed partial class TestSignalJsonSerializerContext : JsonSerializerContext;
