using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithCustomSerializedPayloadType(TestCommandWithCustomSerializedPayloadTypePayload Payload);

    public sealed record TestCommandWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed class TestCommandWithCustomSerializedPayloadTypeHandler : ITestCommandWithCustomSerializedPayloadTypeHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomSerializedPayloadType query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload.Payload + 1 };
        }
    }

    public interface ITestCommandWithCustomSerializedPayloadTypeHandler : ICommandHandler<TestCommandWithCustomSerializedPayloadType, TestCommandResponse>
    {
    }

#pragma warning disable SA1402 // it makes sense to keep these test classes together
    internal sealed class TestCommandWithCustomSerializedPayloadTypePayloadJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestCommandWithCustomSerializedPayloadTypePayload);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Activator.CreateInstance(typeof(TestCommandWithCustomSerializedPayloadTypePayloadJsonConverter)) as JsonConverter;
        }
    }

    internal sealed class TestCommandWithCustomSerializedPayloadTypePayloadJsonConverter : JsonConverter<TestCommandWithCustomSerializedPayloadTypePayload>
    {
        public override TestCommandWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new(reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, TestCommandWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Payload);
        }
    }
}
