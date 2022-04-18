using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [HttpQuery(UsePost = true)]
    public sealed record TestPostQueryWithCustomSerializedPayloadType(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

    public sealed record TestPostQueryWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed class TestPostQueryWithCustomSerializedPayloadTypeHandler : ITestPostQueryWithCustomSerializedPayloadTypeHandler
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithCustomSerializedPayloadType query, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload.Payload + 1 };
        }
    }

    public interface ITestPostQueryWithCustomSerializedPayloadTypeHandler : IQueryHandler<TestPostQueryWithCustomSerializedPayloadType, TestQueryResponse>
    {
    }

#pragma warning disable SA1402 // it makes sense to keep these test classes together
    internal sealed class TestPostQueryWithCustomSerializedPayloadTypePayloadJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestPostQueryWithCustomSerializedPayloadTypePayload);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Activator.CreateInstance(typeof(TestPostQueryWithCustomSerializedPayloadTypePayloadJsonConverter)) as JsonConverter;
        }
    }

    internal sealed class TestPostQueryWithCustomSerializedPayloadTypePayloadJsonConverter : JsonConverter<TestPostQueryWithCustomSerializedPayloadTypePayload>
    {
        public override TestPostQueryWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new(reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, TestPostQueryWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Payload);
        }
    }
}
