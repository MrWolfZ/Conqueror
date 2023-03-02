using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Transport.Http.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class CommandHttpClientTests : TestBase
{
    private const string ErrorPayload = "{\"Message\":\"this is an error\"}";

    private int? customResponseStatusCode;
    private Func<HttpContext, Func<Task>, Task>? middleware;
    private bool useThrowingHttpClient;

    [Test]
    public async Task GivenSuccessfulHttpCall_ReturnsCommandResponse()
    {
        var handler = ResolveOnClient<ITestCommandHandler>();

        var result = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload);
    }

    [Test]
    public async Task GivenFailedHttpCall_ThrowsHttpCommandFailedException()
    {
        var handler = ResolveOnClient<ITestCommandHandler>();

        customResponseStatusCode = StatusCodes.Status402PaymentRequired;

        var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None));

        Assert.IsNotNull(ex);
        Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
        Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
        Assert.AreEqual(ErrorPayload, await ex!.Response!.Content.ReadAsStringAsync());
    }

    [Test]
    public void GivenExceptionDuringHttpCall_ThrowsHttpCommandFailedException()
    {
        useThrowingHttpClient = true;

        var handler = ResolveOnClient<ITestCommandHandler>();

        var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None));

        Assert.IsNotNull(ex);
        Assert.IsNull(ex?.StatusCode);
        Assert.IsNotNull(ex?.InnerException);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithoutResponse_ReturnsNothing()
    {
        var handler = ResolveOnClient<ITestCommandWithoutResponseHandler>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);
    }

    [Test]
    public async Task GivenFailedHttpCallWithoutResponse_ThrowsHttpCommandFailedException()
    {
        var handler = ResolveOnClient<ITestCommandWithoutResponseHandler>();

        customResponseStatusCode = StatusCodes.Status402PaymentRequired;

        var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None));

        Assert.IsNotNull(ex);
        Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
        Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
        Assert.AreEqual(ErrorPayload, await ex!.Response!.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithoutPayload_ReturnsResponse()
    {
        var handler = ResolveOnClient<ITestCommandWithoutPayloadHandler>();

        var result = await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload);
    }

    [Test]
    public async Task GivenFailedHttpCallWithoutPayload_ThrowsHttpCommandFailedException()
    {
        var handler = ResolveOnClient<ITestCommandWithoutPayloadHandler>();

        customResponseStatusCode = StatusCodes.Status402PaymentRequired;

        var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new(), CancellationToken.None));

        Assert.IsNotNull(ex);
        Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
        Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
        Assert.AreEqual(ErrorPayload, await ex!.Response!.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithoutResponseWithoutPayload_ReturnsNothing()
    {
        var handler = ResolveOnClient<ITestCommandWithoutResponseWithoutPayloadHandler>();

        await handler.ExecuteCommand(new(), CancellationToken.None);
    }

    [Test]
    public async Task GivenFailedHttpCallWithoutResponseWithoutPayload_ThrowsHttpCommandFailedException()
    {
        var handler = ResolveOnClient<ITestCommandWithoutResponseWithoutPayloadHandler>();

        customResponseStatusCode = StatusCodes.Status402PaymentRequired;

        var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new(), CancellationToken.None));

        Assert.IsNotNull(ex);
        Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
        Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
        Assert.AreEqual(ErrorPayload, await ex!.Response!.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithCustomSerializedPayloadType_ReturnsCommandResponse()
    {
        var handler = ResolveOnClient<ITestCommandWithCustomSerializedPayloadTypeHandler>();

        var result = await handler.ExecuteCommand(new(new(10)), CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload.Payload);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithCustomPathConvention_ReturnsCommandResponse()
    {
        var handler = ResolveOnClient<ITestCommandWithCustomPathConventionHandler>();

        var result = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithCustomPath_ReturnsCommandResponse()
    {
        var handler = ResolveOnClient<ITestCommandWithCustomPathHandler>();

        var result = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithVersion_ReturnsCommandResponse()
    {
        var handler = ResolveOnClient<ITestCommandWithVersionHandler>();

        var result = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallWithCustomHeaders_ServerReceivesHeaders()
    {
        var handler = ResolveOnClient<ITestCommandWithCustomHeadersHandler>();

        var seenAuthorizationHeader = string.Empty;
        var seenTestHeaderValues = Array.Empty<string?>();

        middleware = (ctx, next) =>
        {
            seenAuthorizationHeader = ctx.Request.Headers.Authorization;
            seenTestHeaderValues = ctx.Request.Headers["test-header"];

            return next();
        };

        _ = await handler.ExecuteCommand(new(10), CancellationToken.None);

        Assert.AreEqual("Basic test", seenAuthorizationHeader);
        CollectionAssert.AreEquivalent(new[] { "value1", "value2" }, seenTestHeaderValues);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallForDelegateHandler_ReturnsCommandResponse()
    {
        var handler = ResolveOnClient<ICommandHandler<TestDelegateCommand, TestDelegateCommandResponse>>();

        var result = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(11, result.Payload);
    }

    [Test]
    public async Task GivenSuccessfulHttpCallForDelegateHandlerWithoutResponse_ReturnsNothing()
    {
        var handler = ResolveOnClient<ICommandHandler<TestDelegateCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new TestHttpCommandPathConvention());
        _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestCommandWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                    .AddConquerorCommandHandler<TestCommandWithoutPayloadHandler>()
                    .AddConquerorCommandHandler<TestCommandWithoutResponseWithoutPayloadHandler>()
                    .AddConquerorCommandHandler<TestCommandWithCustomSerializedPayloadTypeHandler>()
                    .AddConquerorCommandHandler<TestCommandWithCustomPathConventionHandler>()
                    .AddConquerorCommandHandler<TestCommandWithCustomPathHandler>()
                    .AddConquerorCommandHandler<TestCommandWithVersionHandler>()
                    .AddConquerorCommandHandler<TestCommandWithCustomHeadersHandler>()
                    .AddConquerorCommandHandler<NonHttpTestCommandHandler>()
                    .AddConquerorCommandHandler<NonHttpTestCommandWithoutResponseHandler>()
                    .AddConquerorCommandHandlerDelegate<TestDelegateCommand, TestDelegateCommandResponse>((command, _, _) => Task.FromResult(new TestDelegateCommandResponse { Payload = command.Payload + 1 }))
                    .AddConquerorCommandHandlerDelegate<TestDelegateCommandWithoutResponse>((_, _, _) => Task.CompletedTask);
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSHttpClientServices(o =>
        {
            _ = o.UseHttpClient(useThrowingHttpClient ? new ThrowingTestHttpClient() : HttpClient);

            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };

            o.CommandPathConvention = new TestHttpCommandPathConvention();
        });

        var baseAddress = new Uri("http://conqueror.test");

        _ = services.AddConquerorCommandClient<ITestCommandHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithoutResponseHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithoutPayloadHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithoutResponseWithoutPayloadHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithCustomSerializedPayloadTypeHandler>(b => b.UseHttp(baseAddress, o => o.JsonSerializerOptions = new()
                    {
                        Converters = { new TestCommandWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                        PropertyNameCaseInsensitive = true,
                    }))
                    .AddConquerorCommandClient<ITestCommandWithCustomPathConventionHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithCustomPathHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithVersionHandler>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ITestCommandWithCustomHeadersHandler>(b => b.UseHttp(baseAddress, o =>
                    {
                        o.Headers.Authorization = new("Basic", "test");
                        o.Headers.Add("test-header", new[] { "value1", "value2" });
                    }))
                    .AddConquerorCommandClient<ICommandHandler<TestDelegateCommand, TestDelegateCommandResponse>>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ICommandHandler<TestDelegateCommandWithoutResponse>>(b => b.UseHttp(baseAddress));
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (ctx, next) =>
        {
            if (customResponseStatusCode != null)
            {
                ctx.Response.StatusCode = customResponseStatusCode.Value;
                ctx.Response.ContentType = MediaTypeNames.Application.Json;
                await using var streamWriter = new StreamWriter(ctx.Response.Body);
                await streamWriter.WriteAsync(ErrorPayload);
                return;
            }

            await next();
        });

        _ = app.Use((ctx, next) => middleware != null ? middleware(ctx, next) : next());

        _ = app.UseRouting();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpCommand]
    public sealed record TestCommand
    {
        public int Payload { get; init; }
    }

    public sealed record TestCommandResponse
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestCommandWithoutPayload;

    [HttpCommand]
    public sealed record TestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestCommandWithoutResponseWithoutPayload;

    [HttpCommand]
    public sealed record TestCommandWithCustomSerializedPayloadType(TestCommandWithCustomSerializedPayloadTypePayload Payload);

    public sealed record TestCommandWithCustomSerializedPayloadTypeResponse(TestCommandWithCustomSerializedPayloadTypePayload Payload);

    public sealed record TestCommandWithCustomSerializedPayloadTypePayload(int Payload);

    [HttpCommand]
    public sealed record TestCommandWithCustomPathConvention
    {
        public int Payload { get; init; }
    }

    [HttpCommand(Path = "/api/testCommandWithCustomPath")]
    public sealed record TestCommandWithCustomPath
    {
        public int Payload { get; init; }
    }

    [HttpCommand(Version = "v2")]
    public sealed record TestCommandWithVersion
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestCommandWithCustomHeaders(int Payload);

    [HttpCommand]
    public sealed record TestDelegateCommand
    {
        public int Payload { get; init; }
    }

    public sealed record TestDelegateCommandResponse
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestDelegateCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed record NonHttpTestCommand
    {
        public int Payload { get; init; }
    }

    public sealed record NonHttpTestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }

    public interface ITestCommandWithoutPayloadHandler : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
    }

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
    }

    public interface ITestCommandWithoutResponseWithoutPayloadHandler : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
    {
    }

    public interface ITestCommandWithCustomSerializedPayloadTypeHandler : ICommandHandler<TestCommandWithCustomSerializedPayloadType, TestCommandWithCustomSerializedPayloadTypeResponse>
    {
    }

    public interface ITestCommandWithCustomPathConventionHandler : ICommandHandler<TestCommandWithCustomPathConvention, TestCommandResponse>
    {
    }

    public interface ITestCommandWithCustomPathHandler : ICommandHandler<TestCommandWithCustomPath, TestCommandResponse>
    {
    }

    public interface ITestCommandWithVersionHandler : ICommandHandler<TestCommandWithVersion, TestCommandResponse>
    {
    }

    public interface ITestCommandWithCustomHeadersHandler : ICommandHandler<TestCommandWithCustomHeaders, TestCommandResponse>
    {
    }

    public interface INonHttpTestCommandHandler : ICommandHandler<NonHttpTestCommand, TestCommandResponse>
    {
    }

    public interface INonHttpTestCommandWithoutResponseHandler : ICommandHandler<NonHttpTestCommandWithoutResponse>
    {
    }

    public sealed class TestCommandHandler : ITestCommandHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithoutPayloadHandler : ITestCommandWithoutPayloadHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestCommandWithoutResponseHandler : ITestCommandWithoutResponseHandler
    {
        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class TestCommandWithoutResponseWithoutPayloadHandler : ITestCommandWithoutResponseWithoutPayloadHandler
    {
        public async Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class TestCommandWithCustomSerializedPayloadTypeHandler : ITestCommandWithCustomSerializedPayloadTypeHandler
    {
        public async Task<TestCommandWithCustomSerializedPayloadTypeResponse> ExecuteCommand(TestCommandWithCustomSerializedPayloadType command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new(new(command.Payload.Payload + 1));
        }

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestCommandWithCustomSerializedPayloadTypePayload);

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
            }
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestCommandWithCustomSerializedPayloadTypePayload>
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

    public sealed class TestCommandWithCustomPathConventionHandler : ITestCommandWithCustomPathConventionHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomPathConvention command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithCustomPathHandler : ITestCommandWithCustomPathHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomPath command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithVersionHandler : ITestCommandWithVersionHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithVersion command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithCustomHeadersHandler : ITestCommandWithCustomHeadersHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomHeaders command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class NonHttpTestCommandHandler : INonHttpTestCommandHandler
    {
        public Task<TestCommandResponse> ExecuteCommand(NonHttpTestCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class NonHttpTestCommandWithoutResponseHandler : INonHttpTestCommandWithoutResponseHandler
    {
        public Task ExecuteCommand(NonHttpTestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestHttpCommandPathConvention : IHttpCommandPathConvention
    {
        public string? GetCommandPath(Type commandType, HttpCommandAttribute attribute)
        {
            if (commandType != typeof(TestCommandWithCustomPathConvention))
            {
                return null;
            }

            return $"/api/commands/{commandType.Name}FromConvention";
        }
    }

    private sealed class ThrowingTestHttpClient : HttpClient
    {
        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException();
        }
    }
}
