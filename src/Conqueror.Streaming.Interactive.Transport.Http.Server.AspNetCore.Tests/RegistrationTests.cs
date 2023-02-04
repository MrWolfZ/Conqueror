using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    public class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(InteractiveStreamingHttpServerAspNetCoreRegistrationFinalizer)));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorRegistered_FinalizeConquerorRegistrationsAddsFeatureProviders()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            _ = services.FinalizeConquerorRegistrations();

            var applicationPartManager = services.Select(d => d.ImplementationInstance).OfType<ApplicationPartManager>().Single();

            Assert.IsNotNull(applicationPartManager.FeatureProviders.SingleOrDefault(p => p is HttpQueryControllerFeatureProvider));
        }

        [Test]
        public void GivenServiceCollectionWithInteractiveStreamingControllerRegistrationWithoutFinalization_ThrowsExceptionWhenBuildingServiceProviderWithValidation()
        {
            var services = new ServiceCollection();

            _ = services.AddLogging()
                        .AddSingleton<IHostEnvironment>(_ => throw new())
                        .AddSingleton<IWebHostEnvironment>(_ => throw new())
                        .AddSingleton<DiagnosticListener>(_ => throw new())
                        .AddControllers()
                        .AddConquerorInteractiveStreaming();

            // remove some service registrations that fail validation
            _ = services.RemoveAll<IActionInvokerProvider>();

            var ex = Assert.Throws<AggregateException>(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

            Assert.IsInstanceOf<InvalidOperationException>(ex?.InnerException);
            Assert.That(ex?.InnerException?.Message, Contains.Substring("DidYouForgetToCallFinalizeConquerorRegistrations"));
        }

        [Test]
        public void GivenServiceCollectionWithInteractiveStreamingControllerRegistrationWithFinalization_ThrowsExceptionWhenCallingFinalizationAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            _ = services.FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithFinalization_ThrowsExceptionWhenRegisteringInteractiveStreaming()
        {
            var services = new ServiceCollection().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddMvc().AddConquerorInteractiveStreaming());
        }
    }
}
