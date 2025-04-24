using Microsoft.AspNetCore.Builder;

// not using top-level namespace here since we use nested namespaces in tests below
namespace Conqueror.Transport.Http.Tests.Messaging.Server
{
    [TestFixture]
    public sealed partial class MessagingServerRegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithDuplicateMessageName_WhenStartingHost_ThrowsInvalidOperationException()
        {
            Assert.That(() => HttpTransportTestHost.Create(
                            services =>
                            {
                                _ = services.AddRoutingCore()
                                            .AddMessageEndpoints()
                                            .AddMessageHandler<TestMessageHandler>()
                                            .AddMessageHandler<DuplicateMessageName.TestMessageHandler>();
                            }, app => _ = app.UseRouting().UseEndpoints(b => b.MapMessageEndpoints())),
                        Throws.InvalidOperationException.With.Message.Contains("found multiple Conqueror message types with identical path!"));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateMessagePathFromConfig_WhenStartingHost_ThrowsInvalidOperationException()
        {
            Assert.That(() => HttpTransportTestHost.Create(
                            services =>
                            {
                                _ = services.AddRoutingCore()
                                            .AddMessageEndpoints()
                                            .AddMessageHandler<TestMessageHandler>()
                                            .AddMessageHandler<TestMessageWithDuplicatePathFromConfigHandler>();
                            }, app => _ = app.UseRouting().UseEndpoints(b => b.MapMessageEndpoints())),
                        Throws.InvalidOperationException.With.Message.Contains("found multiple Conqueror message types with identical path!"));
        }

        [HttpMessage<TestMessageResponse>]
        private sealed partial record TestMessage;

        private sealed record TestMessageResponse;

        [HttpMessage<TestMessage2Response>]
        private sealed partial record TestMessage2;

        private sealed record TestMessage2Response;

        [HttpMessage<TestMessageResponse>(Path = "test")]
        private sealed partial record TestMessageWithDuplicatePathFromConfig;

        private sealed partial class TestMessageHandler : TestMessage.IHandler
        {
            public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }

        private sealed partial class TestMessageWithDuplicatePathFromConfigHandler : TestMessageWithDuplicatePathFromConfig.IHandler
        {
            public async Task<TestMessageResponse> Handle(TestMessageWithDuplicatePathFromConfig message, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }
    }

#pragma warning disable SA1403 // okay for testing purposes

    namespace DuplicateMessageName
    {
        [HttpMessage<TestMessageResponse>]
        public sealed partial record TestMessage;

        public sealed record TestMessageResponse;

        public sealed partial class TestMessageHandler : TestMessage.IHandler
        {
            public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }
    }
}
