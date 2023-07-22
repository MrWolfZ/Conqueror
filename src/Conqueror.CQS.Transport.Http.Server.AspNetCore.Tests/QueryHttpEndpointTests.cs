using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
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

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpPostQuery_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/queries/testPost", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpQueryWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/queries/testQueryWithoutPayload");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpPostQueryWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/queries/testPostQueryWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpQueryWithComplexPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/queries/TestQueryWithComplexPayload?payload.payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpPostQueryWithCustomSerializedPayloadType_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/queries/testPostQueryWithCustomSerializedPayloadType", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestPostQueryWithCustomSerializedPayloadTypeResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenCustomPathConvention_WhenCallingEndpointsWithPathAccordingToConvention_ReturnsCorrectResponse()
        {
            var response1 = await HttpClient.GetAsync("/api/queries/testQuery3FromConvention?payload=10");
            var response2 = await HttpClient.GetAsync("/api/queries/testQuery4FromConvention");
            await response1.AssertStatusCode(HttpStatusCode.OK);
            await response2.AssertStatusCode(HttpStatusCode.OK);
            var resultString1 = await response1.Content.ReadAsStringAsync();
            var resultString2 = await response2.Content.ReadAsStringAsync();
            var result1 = JsonSerializer.Deserialize<TestQueryResponse>(resultString1, JsonSerializerOptions);
            var result2 = JsonSerializer.Deserialize<TestQueryResponse2>(resultString2, JsonSerializerOptions);

            Assert.That(resultString1, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result1, Is.Not.Null);
            Assert.That(result1!.Payload, Is.EqualTo(11));

            Assert.That(result2, Is.Not.Null);
        }

        [Test]
        public async Task GivenCustomPathConventionAndPostQuery_WhenCallingEndpointsWithPathAccordingToConvention_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/queries/testPostQuery2FromConvention", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenCustomHttpQuery_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/custom/queries/test?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenCustomHttpPostQuery_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/custom/queries/testPost", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenCustomHttpQueryWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/custom/queries/testQueryWithoutPayload");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenCustomHttpPostQueryWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/custom/queries/testPostQueryWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpQueryWithCustomPath_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/testQueryWithCustomPath?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpPostQueryWithCustomPath_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/testPostQueryWithCustomPath", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpQueryWithVersion_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/v2/queries/testQueryWithVersion?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpPostQueryWithVersion_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/v2/queries/testPostQueryWithVersion", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpQueryWithDelegateHandler_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            var response = await HttpClient.GetAsync("/api/queries/testDelegate?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestDelegateQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenHttpPostQueryWithDelegateHandler_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/queries/testPostDelegate", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestDelegateQueryResponse>(resultString, JsonSerializerOptions);

            Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
            applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

            _ = services.AddSingleton(applicationPartManager);

            _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.QueryPathConvention = new TestHttpQueryPathConvention());
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddConquerorQueryHandler<TestQueryHandler2>()
                        .AddConquerorQueryHandler<TestQueryHandler3>()
                        .AddConquerorQueryHandler<TestQueryHandler4>()
                        .AddConquerorQueryHandler<TestQueryHandlerWithoutPayload>()
                        .AddConquerorQueryHandler<TestQueryHandlerWithComplexPayload>()
                        .AddConquerorQueryHandler<TestQueryWithCustomPathHandler>()
                        .AddConquerorQueryHandler<TestQueryWithVersionHandler>()
                        .AddConquerorQueryHandler<TestPostQueryHandler>()
                        .AddConquerorQueryHandler<TestPostQueryHandler2>()
                        .AddConquerorQueryHandler<TestPostQueryHandlerWithoutPayload>()
                        .AddConquerorQueryHandler<TestPostQueryWithCustomSerializedPayloadTypeHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithCustomPathHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithVersionHandler>()
                        .AddConquerorQueryHandlerDelegate<TestDelegateQuery, TestDelegateQueryResponse>(async (command, _, cancellationToken) =>
                        {
                            await Task.Yield();
                            cancellationToken.ThrowIfCancellationRequested();
                            return new() { Payload = command.Payload + 1 };
                        })
                        .AddConquerorQueryHandlerDelegate<TestPostDelegateQuery, TestDelegateQueryResponse>(async (command, _, cancellationToken) =>
                        {
                            await Task.Yield();
                            cancellationToken.ThrowIfCancellationRequested();
                            return new() { Payload = command.Payload + 1 };
                        });
        }

        private JsonSerializerOptions JsonSerializerOptions => Resolve<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseConqueror();
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
        public sealed record TestQuery3
        {
            public int Payload { get; init; }
        }

        [HttpQuery]
        public sealed record TestQuery4
        {
            public int Payload { get; init; }
        }

        [HttpQuery]
        public sealed record TestQueryWithoutPayload;

        [HttpQuery]
        public sealed record TestQueryWithComplexPayload(TestQueryWithComplexPayloadPayload Payload);

        public sealed record TestQueryWithComplexPayloadPayload(int Payload);

        [HttpQuery(Path = "/api/testQueryWithCustomPath")]
        public sealed record TestQueryWithCustomPath
        {
            public int Payload { get; init; }
        }

        [HttpQuery(Version = "v2")]
        public sealed record TestQueryWithVersion
        {
            public int Payload { get; init; }
        }

        [HttpQuery]
        public sealed record TestDelegateQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestDelegateQueryResponse
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery2
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithoutPayload;

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithCustomSerializedPayloadType(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

        public sealed record TestPostQueryWithCustomSerializedPayloadTypeResponse(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

        public sealed record TestPostQueryWithCustomSerializedPayloadTypePayload(int Payload);

        [HttpQuery(UsePost = true, Path = "/api/testPostQueryWithCustomPath")]
        public sealed record TestPostQueryWithCustomPath
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true, Version = "v2")]
        public sealed record TestPostQueryWithVersion
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostDelegateQuery
        {
            public int Payload { get; init; }
        }

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

        public sealed class TestQueryHandler3 : IQueryHandler<TestQuery3, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery3 query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryHandler4 : IQueryHandler<TestQuery4, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery4 query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
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

        public sealed class TestQueryHandlerWithComplexPayload : IQueryHandler<TestQueryWithComplexPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithComplexPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload.Payload + 1 };
            }
        }

        public sealed class TestQueryWithCustomPathHandler : IQueryHandler<TestQueryWithCustomPath, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithCustomPath query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryWithVersionHandler : IQueryHandler<TestQueryWithVersion, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithVersion query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
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

        public sealed class TestPostQueryHandler2 : IQueryHandler<TestPostQuery2, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQuery2 query, CancellationToken cancellationToken = default)
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

        public sealed class TestPostQueryWithCustomPathHandler : IQueryHandler<TestPostQueryWithCustomPath, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithCustomPath query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestPostQueryWithVersionHandler : IQueryHandler<TestPostQueryWithVersion, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithVersion query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        private sealed class TestHttpQueryPathConvention : IHttpQueryPathConvention
        {
            public string? GetQueryPath(Type queryType, HttpQueryAttribute attribute)
            {
                if (queryType != typeof(TestQuery3) && queryType != typeof(TestQuery4) && queryType != typeof(TestPostQuery2))
                {
                    return null;
                }

                return $"/api/queries/{queryType.Name}FromConvention";
            }
        }

        [ApiController]
        private sealed class TestHttpQueryController : ControllerBase
        {
            private readonly IQueryHandler<TestQuery, TestQueryResponse> queryHandler;
            private readonly IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> queryWithoutPayloadHandler;
            private readonly IQueryHandler<TestPostQuery, TestQueryResponse> postQueryHandler;
            private readonly IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse> postQueryWithoutPayloadHandler;

            public TestHttpQueryController(IQueryHandler<TestQuery, TestQueryResponse> queryHandler,
                                           IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> queryWithoutPayloadHandler,
                                           IQueryHandler<TestPostQuery, TestQueryResponse> postQueryHandler,
                                           IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse> postQueryWithoutPayloadHandler)
            {
                this.queryHandler = queryHandler;
                this.queryWithoutPayloadHandler = queryWithoutPayloadHandler;
                this.postQueryHandler = postQueryHandler;
                this.postQueryWithoutPayloadHandler = postQueryWithoutPayloadHandler;
            }

            [HttpGet("/api/custom/queries/test")]
            public Task<TestQueryResponse> ExecuteTestQuery([FromQuery] TestQuery query, CancellationToken cancellationToken)
            {
                return queryHandler.ExecuteQuery(query, cancellationToken);
            }

            [HttpGet("/api/custom/queries/testQueryWithoutPayload")]
            public Task<TestQueryResponse> ExecuteTestQueryWithoutPayload(CancellationToken cancellationToken)
            {
                return queryWithoutPayloadHandler.ExecuteQuery(new(), cancellationToken);
            }

            [HttpPost("/api/custom/queries/testPost")]
            public Task<TestQueryResponse> ExecuteTestQueryWithoutResponse(TestPostQuery query, CancellationToken cancellationToken)
            {
                return postQueryHandler.ExecuteQuery(query, cancellationToken);
            }

            [HttpPost("/api/custom/queries/testPostQueryWithoutPayload")]
            public Task<TestQueryResponse> ExecuteTestQueryWithoutResponse(CancellationToken cancellationToken)
            {
                return postQueryWithoutPayloadHandler.ExecuteQuery(new(), cancellationToken);
            }
        }

        private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public override string Name => nameof(TestControllerApplicationPart);

            public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestHttpQueryController).GetTypeInfo() };
        }

        private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpQueryController);
        }
    }
}
