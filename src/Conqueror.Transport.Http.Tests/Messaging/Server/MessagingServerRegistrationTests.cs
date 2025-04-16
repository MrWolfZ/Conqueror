using System.Diagnostics;
using Conqueror.Transport.Http.Server.AspNetCore;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;

// not using top-level namespace here since we use nested namespaces in tests below
namespace Conqueror.Transport.Http.Tests.Messaging.Server
{
    [TestFixture]
    public sealed partial class MessagingServerRegistrationTests
    {
        [Test]
        public void GivenServiceCollection_WhenAddingControllers_AddsOwnTypesAndCoreTypes()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers().AddMessageControllers();

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(Debugger.IsAttached ? 2 : 1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && (d.ImplementationType?.IsGenericType ?? false)
                                 && d.ImplementationType.GetGenericTypeDefinition() == typeof(HttpMessageEndpointApplicationModelProvider<,>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType.IsGenericType
                                 && d.ServiceType.GetGenericTypeDefinition() == typeof(HttpMessageEndpointApplicationModelProvider<,>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionApplicationModelProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageDuplicatePathValidationApplicationModelProvider)));

            // instead of asserting on all types, we just assert on one, assuming that all others will be registered as normal
            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageClients)));
        }

        [Test]
        public void GivenServiceCollection_WhenAddingController_AddsOwnTypesAndCoreTypes()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers().AddMessageController<TestMessage>();

            Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(Debugger.IsAttached ? 2 : 1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointApplicationModelProvider<TestMessage, TestMessageResponse>)));

            Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(
                            d => d.ServiceType.IsGenericType
                                 && d.ServiceType.GetGenericTypeDefinition() == typeof(HttpMessageEndpointApplicationModelProvider<,>)));

            Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionApplicationModelProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageDuplicatePathValidationApplicationModelProvider)));

            // instead of asserting on all types, we just assert on one, assuming that all others will be registered as normal
            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageClients)));
        }

        [Test]
        public void GivenServiceCollection_WhenAddingControllersAndSpecificController_AddsOwnTypesAndCoreTypes()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers().AddMessageControllers().AddMessageController<TestMessage>();

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(Debugger.IsAttached ? 2 : 1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointApplicationModelProvider<TestMessage, TestMessageResponse>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionApplicationModelProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType.IsGenericType
                                 && d.ServiceType.GetGenericTypeDefinition() == typeof(HttpMessageEndpointApplicationModelProvider<,>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageDuplicatePathValidationApplicationModelProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(HttpMessageControllerRegistration)));

            // instead of asserting on all types, we just assert on one, assuming that all others will be registered as normal
            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageClients)));
        }

        [Test]
        public void GivenServiceCollection_WhenAddingControllersOrSpecificControllerMultipleTimes_AddsOwnTypesAndCoreTypesOnlyOnce()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers()
                        .AddMessageController<TestMessage>()
                        .AddMessageControllers()
                        .AddMessageController<TestMessage2>()
                        .AddMessageControllers();

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IStartupFilter)
                                 && d.ImplementationType == typeof(HttpMessageEndpointConfigurationStartupFilter)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(Debugger.IsAttached ? 2 : 1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IActionDescriptorChangeProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointApplicationModelProvider<TestMessage, TestMessageResponse>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointApplicationModelProvider<TestMessage2, TestMessage2Response>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageEndpointReflectionApplicationModelProvider)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType.IsGenericType
                                 && d.ServiceType.GetGenericTypeDefinition() == typeof(HttpMessageEndpointApplicationModelProvider<,>)));

            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(
                            d => d.ServiceType == typeof(IApplicationModelProvider)
                                 && d.ImplementationType == typeof(HttpMessageDuplicatePathValidationApplicationModelProvider)));

            Assert.That(services, Has.Exactly(2).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(HttpMessageControllerRegistration)));

            // instead of asserting on all types, we just assert on one, assuming that all others will be registered as normal
            Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageClients)));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateMessageName_WhenStartingHost_ThrowsInvalidOperationException()
        {
            Assert.That(() => HttpTransportTestHost.Create(
                            services =>
                            {
                                _ = services.AddControllers().AddMessageControllers();

                                _ = services.AddMessageHandler<TestMessageHandler>()
                                            .AddMessageHandler<DuplicateMessageName.TestMessageHandler>();
                            }, app => _ = app.UseRouting().UseEndpoints(b => b.MapControllers())),
                        Throws.InvalidOperationException.With.Message.Contains("found multiple Conqueror message types with identical path!"));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateMessageNameFromExplicitControllerRegistration_WhenStartingHost_ThrowsInvalidOperationException()
        {
            Assert.That(() => HttpTransportTestHost.Create(
                            services =>
                            {
                                _ = services.AddControllers()
                                            .AddMessageController<TestMessage>()
                                            .AddMessageController<DuplicateMessageName.TestMessage>();
                            }, app => _ = app.UseRouting().UseEndpoints(b => b.MapControllers())),
                        Throws.InvalidOperationException.With.Message.Contains("found multiple Conqueror message types with identical path!"));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateMessageNameFromDelegate_WhenStartingHost_ThrowsInvalidOperationException()
        {
            Assert.That(() => HttpTransportTestHost.Create(
                            services =>
                            {
                                _ = services.AddControllers().AddMessageControllers();

                                _ = services.AddMessageHandler<TestMessageHandler>()
                                            .AddMessageHandlerDelegate<DuplicateMessageName.TestMessage, DuplicateMessageName.TestMessageResponse>(
                                                (_, _, _) => new());
                            }, app => _ = app.UseRouting().UseEndpoints(b => b.MapControllers())),
                        Throws.InvalidOperationException.With.Message.Contains("found multiple Conqueror message types with identical path!"));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateMessagePathFromConfig_WhenStartingHost_ThrowsInvalidOperationException()
        {
            Assert.That(() => HttpTransportTestHost.Create(
                            services =>
                            {
                                _ = services.AddControllers().AddMessageControllers();

                                _ = services.AddMessageHandler<TestMessageHandler>()
                                            .AddMessageHandler<TestMessageWithDuplicatePathFromConfigHandler>();
                            }, app => _ = app.UseRouting().UseEndpoints(b => b.MapControllers())),
                        Throws.InvalidOperationException.With.Message.Contains("found multiple Conqueror message types with identical path!"));
        }

        [HttpMessage]
        [Message<TestMessageResponse>]
        private sealed partial record TestMessage;

        private sealed record TestMessageResponse;

        [HttpMessage]
        [Message<TestMessage2Response>]
        private sealed partial record TestMessage2;

        private sealed record TestMessage2Response;

        [HttpMessage(Path = "test")]
        [Message<TestMessageResponse>]
        private sealed partial record TestMessageWithDuplicatePathFromConfig;

        private sealed class TestMessageHandler : TestMessage.IHandler
        {
            public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }

        private sealed class TestMessageWithDuplicatePathFromConfigHandler : TestMessageWithDuplicatePathFromConfig.IHandler
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
        [HttpMessage]
        [Message<TestMessageResponse>]
        public sealed partial record TestMessage;

        public sealed record TestMessageResponse;

        public sealed class TestMessageHandler : TestMessage.IHandler
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
