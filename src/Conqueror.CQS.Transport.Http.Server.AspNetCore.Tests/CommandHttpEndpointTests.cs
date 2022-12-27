using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    public sealed class CommandHttpEndpointTests : TestBase
    {
        [Test]
        public async Task HttpCommandTest()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/commands/test", new TestCommand { Payload = 10 });
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestCommandResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpCommandWithoutResponseTest()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/commands/testCommandWithoutResponse", new TestCommand { Payload = 10 });
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        [Test]
        public async Task HttpCommandWithoutPayloadTest()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestCommandResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpCommandWithoutResponseWithoutPayloadTest()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutResponseWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandHandlerWithoutPayload>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutPayload>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034
        
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
        public sealed record TestCommandWithoutPayload;

        [HttpCommand]
        public sealed record TestCommandWithoutResponse
        {
            public int Payload { get; init; }
        }
    
        [HttpCommand]
        public sealed record TestCommandWithoutResponseWithoutPayload;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
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
    }
}
