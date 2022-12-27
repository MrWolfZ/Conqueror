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

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpCommandWithoutResponse_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutResponse", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GivenHttpCommandWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpCommandWithoutResponseWithoutPayload_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutResponseWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GivenHttpCommandWithCustomSerializedPayloadType_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithCustomSerializedPayloadType", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestCommandWithCustomSerializedPayloadTypeResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload.Payload);
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

            Assert.AreEqual("{\"payload\":11}", resultString1);
            Assert.IsNotNull(result1);
            Assert.AreEqual(11, result1!.Payload);

            Assert.IsNotNull(result2);
        }
        
        [Test]
        public async Task GivenHttpCommandWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/custom/commands/test", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpCommandWithoutResponseWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = CreateJsonStringContent("{\"payload\":10}");
            var response = await HttpClient.PostAsync("/api/custom/commands/testCommandWithoutResponse", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GivenHttpCommandWithoutPayloadWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/custom/commands/testCommandWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TestCommandResponse>(resultString, JsonSerializerOptions);

            Assert.AreEqual("{\"payload\":11}", resultString);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenHttpCommandWithoutResponseWithoutPayloadWithCustomController_WhenCallingEndpoint_ReturnsCorrectResponse()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/custom/commands/testCommandWithoutResponseWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
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

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandler3>()
                        .AddTransient<TestCommandHandler4>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandHandlerWithoutPayload>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutPayload>()
                        .AddTransient<TestCommandWithCustomSerializedPayloadTypeHandler>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

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

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        public interface ITestCommandWithCustomSerializedPayloadTypeHandler : ICommandHandler<TestCommandWithCustomSerializedPayloadType, TestCommandWithCustomSerializedPayloadTypeResponse>
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
        private sealed class TestHttpCommandController : ControllerBase
        {
            [HttpPost("/api/custom/commands/test")]
            public Task<TestCommandResponse> ExecuteTestCommand(TestCommand command, CancellationToken cancellationToken)
            {
                return HttpCommandExecutor.ExecuteCommand<TestCommand, TestCommandResponse>(HttpContext, command, cancellationToken);
            }

            [HttpPost("/api/custom/commands/testCommandWithoutPayload")]
            public Task<TestCommandResponse> ExecuteTestCommandWithoutPayload(CancellationToken cancellationToken)
            {
                return HttpCommandExecutor.ExecuteCommand<TestCommandWithoutPayload, TestCommandResponse>(HttpContext, cancellationToken);
            }

            [HttpPost("/api/custom/commands/testCommandWithoutResponse")]
            public Task ExecuteTestCommandWithoutResponse(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                return HttpCommandExecutor.ExecuteCommand(HttpContext, command, cancellationToken);
            }

            [HttpPost("/api/custom/commands/testCommandWithoutResponseWithoutPayload")]
            public Task ExecuteTestCommandWithoutPayloadWithoutResponse(CancellationToken cancellationToken)
            {
                return HttpCommandExecutor.ExecuteCommand<TestCommandWithoutResponseWithoutPayload>(HttpContext, cancellationToken);
            }
        }

        private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public override string Name => nameof(TestControllerApplicationPart);

            public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestHttpCommandController).GetTypeInfo() };
        }

        private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpCommandController);
        }
    }
}
