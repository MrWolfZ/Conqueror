using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
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

    [Test]
    public async Task GivenHttpQueryWithHandlerWithMiddleware_WhenCallingEndpoint_MiddlewareContextContainsCorrectTransportType()
    {
        var response = await HttpClient.GetAsync("/api/queries/testWithMiddleware?payload=10");
        await response.AssertStatusCode(HttpStatusCode.OK);

        var seenTransportType = Resolve<TestQueryMiddleware<TestWithMiddlewareQuery, TestQueryResponse>>().SeenTransportType;
        Assert.That(seenTransportType?.IsHttp(), Is.True);
        Assert.That(seenTransportType?.Role, Is.EqualTo(QueryTransportRole.Server));
    }

    [Test]
    public async Task GivenHttpPostQueryWithHandlerWithMiddleware_WhenCallingEndpoint_MiddlewareContextContainsCorrectTransportType()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/queries/testPostWithMiddleware", content);
        await response.AssertStatusCode(HttpStatusCode.OK);

        var seenTransportType = Resolve<TestQueryMiddleware<TestPostWithMiddlewareQuery, TestQueryResponse>>().SeenTransportType;
        Assert.That(seenTransportType?.IsHttp(), Is.True);
        Assert.That(seenTransportType?.Role, Is.EqualTo(QueryTransportRole.Server));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);

        _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.QueryPathConvention = new TestHttpQueryPathConvention());
        _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

        _ = services.AddSingleton<TestQueryMiddleware<TestWithMiddlewareQuery, TestQueryResponse>>()
                    .AddSingleton<TestQueryMiddleware<TestPostWithMiddlewareQuery, TestQueryResponse>>()
                    .AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestQueryHandler2>()
                    .AddConquerorQueryHandler<TestQueryHandler3>()
                    .AddConquerorQueryHandler<TestQueryHandler4>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithoutPayload>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithMiddleware>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithComplexPayload>()
                    .AddConquerorQueryHandler<TestQueryWithCustomPathHandler>()
                    .AddConquerorQueryHandler<TestQueryWithVersionHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler2>()
                    .AddConquerorQueryHandler<TestPostQueryHandlerWithoutPayload>()
                    .AddConquerorQueryHandler<TestPostQueryHandlerWithMiddleware>()
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
        return new(content, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
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

    [HttpQuery]
    public sealed record TestWithMiddlewareQuery
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

    [HttpQuery(UsePost = true)]
    public sealed record TestPostWithMiddlewareQuery
    {
        public int Payload { get; init; }
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>;

    public interface ITestPostQueryWithCustomSerializedPayloadTypeHandler : IQueryHandler<TestPostQueryWithCustomSerializedPayloadType, TestPostQueryWithCustomSerializedPayloadTypeResponse>;

    public sealed class TestQueryHandler : ITestQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
    {
        public Task<TestQueryResponse2> Handle(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestQueryHandler3 : IQueryHandler<TestQuery3, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery3 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryHandler4 : IQueryHandler<TestQuery4, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery4 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestQueryHandlerWithMiddleware : IQueryHandler<TestWithMiddlewareQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestWithMiddlewareQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }

        public static void ConfigurePipeline(IQueryPipeline<TestWithMiddlewareQuery, TestQueryResponse> pipeline) =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestQueryMiddleware<TestWithMiddlewareQuery, TestQueryResponse>>());
    }

    public sealed class TestQueryHandlerWithComplexPayload : IQueryHandler<TestQueryWithComplexPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithComplexPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload.Payload + 1 };
        }
    }

    public sealed class TestQueryWithCustomPathHandler : IQueryHandler<TestQueryWithCustomPath, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithCustomPath query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryWithVersionHandler : IQueryHandler<TestQueryWithVersion, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithVersion query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryHandler : ITestPostQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestPostQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryHandler2 : IQueryHandler<TestPostQuery2, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQuery2 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryHandlerWithoutPayload : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithoutPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestPostQueryHandlerWithMiddleware : IQueryHandler<TestPostWithMiddlewareQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostWithMiddlewareQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }

        public static void ConfigurePipeline(IQueryPipeline<TestPostWithMiddlewareQuery, TestQueryResponse> pipeline) =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestQueryMiddleware<TestPostWithMiddlewareQuery, TestQueryResponse>>());
    }

    public sealed class TestPostQueryWithCustomSerializedPayloadTypeHandler : ITestPostQueryWithCustomSerializedPayloadTypeHandler
    {
        public async Task<TestPostQueryWithCustomSerializedPayloadTypeResponse> Handle(TestPostQueryWithCustomSerializedPayloadType query, CancellationToken cancellationToken = default)
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
        public async Task<TestQueryResponse> Handle(TestPostQueryWithCustomPath query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryWithVersionHandler : IQueryHandler<TestPostQueryWithVersion, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithVersion query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    private sealed class TestQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
    {
        public QueryTransportType? SeenTransportType { get; private set; }

        public Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            SeenTransportType = ctx.TransportType;
            return ctx.Next(ctx.Query, ctx.CancellationToken);
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
    private sealed class TestHttpQueryController(
        IQueryHandler<TestQuery, TestQueryResponse> queryHandler,
        IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> queryWithoutPayloadHandler,
        IQueryHandler<TestPostQuery, TestQueryResponse> postQueryHandler,
        IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse> postQueryWithoutPayloadHandler)
        : ControllerBase
    {
        [HttpGet("/api/custom/queries/test")]
        public Task<TestQueryResponse> ExecuteTestQuery([FromQuery] TestQuery query, CancellationToken cancellationToken)
        {
            return queryHandler.Handle(query, cancellationToken);
        }

        [HttpGet("/api/custom/queries/testQueryWithoutPayload")]
        public Task<TestQueryResponse> ExecuteTestQueryWithoutPayload(CancellationToken cancellationToken)
        {
            return queryWithoutPayloadHandler.Handle(new(), cancellationToken);
        }

        [HttpPost("/api/custom/queries/testPost")]
        public Task<TestQueryResponse> ExecuteTestQueryWithoutResponse(TestPostQuery query, CancellationToken cancellationToken)
        {
            return postQueryHandler.Handle(query, cancellationToken);
        }

        [HttpPost("/api/custom/queries/testPostQueryWithoutPayload")]
        public Task<TestQueryResponse> ExecuteTestQueryWithoutResponse(CancellationToken cancellationToken)
        {
            return postQueryWithoutPayloadHandler.Handle(new(), cancellationToken);
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = [typeof(TestHttpQueryController).GetTypeInfo()];
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpQueryController);
    }
}
