using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
#if NET7_0_OR_GREATER
using System.Net.Http.Headers;
#endif

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public sealed class QueryHttpEndpointTests : TestBase
    {
        [Test]
        public async Task GivenHttpQuery_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/queries/test?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpPostQuery_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/queries/testPost", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpQueryWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/queries/testQueryWithoutPayload");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpPostQueryWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/queries/testPostQueryWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpQueryWithCustomSerializedPayloadType_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/queries/testPostQueryWithCustomSerializedPayloadType", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestPostQueryWithCustomSerializedPayloadTypeResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload.Payload);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestPostQueryHandlerWithoutPayload>()
                        .AddTransient<TestPostQueryWithCustomSerializedPayloadTypeHandler>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        private JsonSerializerOptions JsonSerializerOptions => Resolve<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        private static StringContent CreateJsonStringContent(string content)
        {
#if NET7_0_OR_GREATER
            var stringContent = new StringContent(content, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
#else
            var stringContent = new StringContent(content, null, MediaTypeNames.Application.Json);
#endif
            return stringContent;
        }

        [HttpQuery]
        public sealed record TestQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestQueryResponse
        {
            public int Payload { get; init; }
        }

        [HttpQuery]
        public sealed record TestQuery2;

        public sealed record TestQueryResponse2;

        [HttpQuery]
        public sealed record TestQueryWithoutPayload;

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithoutPayload;

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithCustomSerializedPayloadType(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

        public sealed record TestPostQueryWithCustomSerializedPayloadTypeResponse(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

        public sealed record TestPostQueryWithCustomSerializedPayloadTypePayload(int Payload);

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
        {
        }

        public interface ITestPostQueryWithCustomSerializedPayloadTypeHandler : IQueryHandler<TestPostQueryWithCustomSerializedPayloadType, TestPostQueryWithCustomSerializedPayloadTypeResponse>
        {
        }

        public sealed class TestQueryHandler : ITestQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
        {
            public Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestPostQueryHandler : ITestPostQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestPostQueryHandlerWithoutPayload : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestPostQueryWithCustomSerializedPayloadTypeHandler : ITestPostQueryWithCustomSerializedPayloadTypeHandler
        {
            public async Task<TestPostQueryWithCustomSerializedPayloadTypeResponse> ExecuteQuery(TestPostQueryWithCustomSerializedPayloadType query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new(new(query.Payload.Payload + 1));
            }

            internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
            {
                public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestPostQueryWithCustomSerializedPayloadTypePayload);

                public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
                {
                    return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
                }
            }

            internal sealed class PayloadJsonConverter : JsonConverter<TestPostQueryWithCustomSerializedPayloadTypePayload>
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
    }
}
