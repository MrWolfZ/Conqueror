using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public sealed class CommandHttpClientTests : TestBase
    {
        private const string ErrorPayload = "{\"Message\":\"this is an error\"}";

        private int? customResponseStatusCode;

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
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
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
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
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
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
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
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
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

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new TestHttpCommandPathConvention());
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestCommandWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandWithoutResponseHandler>()
                        .AddTransient<TestCommandWithoutPayloadHandler>()
                        .AddTransient<TestCommandWithoutResponseWithoutPayloadHandler>()
                        .AddTransient<TestCommandWithCustomSerializedPayloadTypeHandler>()
                        .AddTransient<TestCommandWithCustomPathConventionHandler>()
                        .AddTransient<NonHttpTestCommandHandler>()
                        .AddTransient<NonHttpTestCommandWithoutResponseHandler>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQSHttpClientServices(o =>
            {
                o.HttpClientFactory = uri =>
                    throw new InvalidOperationException(
                        $"during tests all clients should be explicitly configured with the test http client; got request to create http client for base address '{uri}'");

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };

                o.CommandPathConvention = new TestHttpCommandPathConvention();
            });

            _ = services.AddConquerorCommandClient<ITestCommandHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorCommandClient<ITestCommandWithoutResponseHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorCommandClient<ITestCommandWithoutPayloadHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorCommandClient<ITestCommandWithoutResponseWithoutPayloadHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorCommandClient<ITestCommandWithCustomSerializedPayloadTypeHandler>(b => b.UseHttp(HttpClient, o => o.JsonSerializerOptions = new()
                        {
                            Converters = { new TestCommandWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                            PropertyNameCaseInsensitive = true,
                        }))
                        .AddConquerorCommandClient<ITestCommandWithCustomPathConventionHandler>(b => b.UseHttp(HttpClient));

            _ = services.FinalizeConquerorRegistrations();
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
    }
}
