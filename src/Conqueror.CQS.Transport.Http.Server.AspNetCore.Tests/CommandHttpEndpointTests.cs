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
public sealed class CommandHttpEndpointTests : TestBase
{
    [Test]
    public async Task GivenHttpCommand_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/test", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithoutResponse_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutResponse", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GivenHttpCommandWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = new StringContent(string.Empty);
        var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutPayload", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithoutResponseWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = new StringContent(string.Empty);
        var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutResponseWithoutPayload", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GivenHttpCommandWithCustomSerializedPayloadType_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/testCommandWithCustomSerializedPayloadType", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandWithCustomSerializedPayloadTypeResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenCustomPathConvention_WhenCallingEndpointsWithPathAccordingToConvention_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response1 = await HttpClient.PostAsync("/api/commands/testCommand3FromConvention", content);
        var response2 = await HttpClient.PostAsync("/api/commands/testCommand4FromConvention", content);
        await response1.AssertStatusCode(HttpStatusCode.OK);
        await response2.AssertStatusCode(HttpStatusCode.OK);
        var resultString1 = await response1.Content.ReadAsStringAsync();
        var resultString2 = await response2.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<TestCommandResponse>(resultString1, JsonSerializerOptions);
        var result2 = JsonSerializer.Deserialize<TestCommandResponse>(resultString2, JsonSerializerOptions);

        Assert.That(resultString1, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result1, Is.Not.Null);
        Assert.That(result1!.Payload, Is.EqualTo(11));

        Assert.That(result2, Is.Not.Null);
    }

    [Test]
    public async Task GivenHttpCommandWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/custom/commands/test", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithoutResponseWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/custom/commands/testCommandWithoutResponse", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GivenHttpCommandWithoutPayloadWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = new StringContent(string.Empty);
        var response = await HttpClient.PostAsync("/api/custom/commands/testCommandWithoutPayload", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithoutResponseWithoutPayloadWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = new StringContent(string.Empty);
        var response = await HttpClient.PostAsync("/api/custom/commands/testCommandWithoutResponseWithoutPayload", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GivenHttpCommandWithCustomPath_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/testCommandWithCustomPath", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithVersion_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/v2/commands/testCommandWithVersion", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithDelegateHandler_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/testDelegate", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var resultString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TestDelegateCommandResponse>(resultString, JsonSerializerOptions);

        Assert.That(resultString, Is.EqualTo("{\"payload\":11}"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHttpCommandWithoutResponseWithDelegateHandler_WhenCallingEndpoint_ReturnsCorrectResponse()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/testDelegateCommandWithoutResponse", content);
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GivenHttpCommandWithHandlerWithMiddleware_WhenCallingEndpoint_MiddlewareContextContainsCorrectTransportType()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/testWithMiddleware", content);
        await response.AssertStatusCode(HttpStatusCode.OK);

        var seenTransportType = Resolve<TestCommandMiddleware<TestWithMiddlewareCommand, TestCommandResponse>>().SeenTransportType;
        Assert.That(seenTransportType?.IsHttp(), Is.True);
        Assert.That(seenTransportType?.Role, Is.EqualTo(CommandTransportRole.Server));
    }

    [Test]
    public async Task GivenHttpCommandWithoutResponseWithHandlerWithMiddleware_WhenCallingEndpoint_MiddlewareContextContainsCorrectTransportType()
    {
        using var content = CreateJsonStringContent("{\"payload\":10}");
        var response = await HttpClient.PostAsync("/api/commands/testWithMiddlewareWithoutResponse", content);
        await response.AssertStatusCode(HttpStatusCode.OK);

        var seenTransportType = Resolve<TestCommandMiddleware<TestWithMiddlewareWithoutResponseCommand, UnitCommandResponse>>().SeenTransportType;
        Assert.That(seenTransportType?.IsHttp(), Is.True);
        Assert.That(seenTransportType?.Role, Is.EqualTo(CommandTransportRole.Server));
    }

    private JsonSerializerOptions JsonSerializerOptions => Resolve<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);

        _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new TestHttpCommandPathConvention());
        _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestCommandWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

        _ = services.AddSingleton<TestCommandMiddleware<TestWithMiddlewareCommand, TestCommandResponse>>()
                    .AddSingleton<TestCommandMiddleware<TestWithMiddlewareWithoutResponseCommand, UnitCommandResponse>>()
                    .AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandHandler2>()
                    .AddConquerorCommandHandler<TestCommandHandler3>()
                    .AddConquerorCommandHandler<TestCommandHandler4>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutPayload>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithMiddleware>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutPayload>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithMiddlewareWithoutResponse>()
                    .AddConquerorCommandHandler<TestCommandWithCustomSerializedPayloadTypeHandler>()
                    .AddConquerorCommandHandler<TestCommandWithCustomPathHandler>()
                    .AddConquerorCommandHandler<TestCommandWithVersionHandler>()
                    .AddConquerorCommandHandlerDelegate<TestDelegateCommand, TestDelegateCommandResponse>(async (command, _, cancellationToken) =>
                    {
                        await Task.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                        return new() { Payload = command.Payload + 1 };
                    })
                    .AddConquerorCommandHandlerDelegate<TestDelegateCommandWithoutResponse>(async (_, _, cancellationToken) =>
                    {
                        await Task.Yield();
                        cancellationToken.ThrowIfCancellationRequested();
                    });
    }

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
    public sealed record TestCommand2;

    public sealed record TestCommandResponse2;

    [HttpCommand]
    public sealed record TestCommand3
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestCommand4
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

    [HttpCommand]
    public sealed record TestWithMiddlewareCommand
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestWithMiddlewareWithoutResponseCommand
    {
        public int Payload { get; init; }
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandWithCustomSerializedPayloadTypeHandler : ICommandHandler<TestCommandWithCustomSerializedPayloadType, TestCommandWithCustomSerializedPayloadTypeResponse>;

    public sealed class TestCommandHandler : ITestCommandHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
    {
        public Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestCommandHandler3 : ICommandHandler<TestCommand3, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand3 command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandHandler4 : ICommandHandler<TestCommand4, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand4 command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestCommandHandlerWithMiddleware : ICommandHandler<TestWithMiddlewareCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestWithMiddlewareCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new();
        }

        public static void ConfigurePipeline(ICommandPipeline<TestWithMiddlewareCommand, TestCommandResponse> pipeline) =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestCommandMiddleware<TestWithMiddlewareCommand, TestCommandResponse>>());
    }

    public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
    {
        public async Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class TestCommandHandlerWithMiddlewareWithoutResponse : ICommandHandler<TestWithMiddlewareWithoutResponseCommand>
    {
        public async Task ExecuteCommand(TestWithMiddlewareWithoutResponseCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }

        public static void ConfigurePipeline(ICommandPipeline<TestWithMiddlewareWithoutResponseCommand> pipeline) =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestCommandMiddleware<TestWithMiddlewareWithoutResponseCommand, UnitCommandResponse>>());
    }

    public sealed class TestCommandWithCustomSerializedPayloadTypeHandler : ITestCommandWithCustomSerializedPayloadTypeHandler
    {
        public async Task<TestCommandWithCustomSerializedPayloadTypeResponse> ExecuteCommand(TestCommandWithCustomSerializedPayloadType query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new(new(query.Payload.Payload + 1));
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

    public sealed class TestCommandWithCustomPathHandler : ICommandHandler<TestCommandWithCustomPath, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomPath command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithVersionHandler : ICommandHandler<TestCommandWithVersion, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithVersion command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    private sealed class TestCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public CommandTransportType? SeenTransportType { get; private set; }

        public Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            SeenTransportType = ctx.TransportType;
            return ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestHttpCommandPathConvention : IHttpCommandPathConvention
    {
        public string? GetCommandPath(Type commandType, HttpCommandAttribute attribute)
        {
            if (commandType != typeof(TestCommand3) && commandType != typeof(TestCommand4))
            {
                return null;
            }

            return $"/api/commands/{commandType.Name}FromConvention";
        }
    }

    [ApiController]
    private sealed class TestHttpCommandController(
        ICommandHandler<TestCommand, TestCommandResponse> commandHandler,
        ICommandHandler<TestCommandWithoutPayload, TestCommandResponse> commandWithoutPayloadHandler,
        ICommandHandler<TestCommandWithoutResponse> commandWithoutResponseHandler,
        ICommandHandler<TestCommandWithoutResponseWithoutPayload> commandWithoutResponseWithoutPayloadHandler)
        : ControllerBase
    {
        [HttpPost("/api/custom/commands/test")]
        public Task<TestCommandResponse> ExecuteTestCommand(TestCommand command, CancellationToken cancellationToken)
        {
            return commandHandler.ExecuteCommand(command, cancellationToken);
        }

        [HttpPost("/api/custom/commands/testCommandWithoutPayload")]
        public Task<TestCommandResponse> ExecuteTestCommandWithoutPayload(CancellationToken cancellationToken)
        {
            return commandWithoutPayloadHandler.ExecuteCommand(new(), cancellationToken);
        }

        [HttpPost("/api/custom/commands/testCommandWithoutResponse")]
        public Task ExecuteTestCommandWithoutResponse(TestCommandWithoutResponse command, CancellationToken cancellationToken)
        {
            return commandWithoutResponseHandler.ExecuteCommand(command, cancellationToken);
        }

        [HttpPost("/api/custom/commands/testCommandWithoutResponseWithoutPayload")]
        public Task ExecuteTestCommandWithoutPayloadWithoutResponse(CancellationToken cancellationToken)
        {
            return commandWithoutResponseWithoutPayloadHandler.ExecuteCommand(new(), cancellationToken);
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = [typeof(TestHttpCommandController).GetTypeInfo()];
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpCommandController);
    }
}
