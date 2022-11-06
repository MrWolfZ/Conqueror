using Microsoft.AspNetCore.Mvc.ApplicationParts;

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

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(InteractiveStreamingAspNetCoreServerServiceCollectionConfigurator)));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorRegistered_ConfigureConquerorAddsFeatureProviders()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            _ = services.ConfigureConqueror();

            var applicationPartManager = services.Select(d => d.ImplementationInstance).OfType<ApplicationPartManager>().Single();

            Assert.IsNotNull(applicationPartManager.FeatureProviders.SingleOrDefault(p => p is HttpQueryControllerFeatureProvider));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyConfigured_ConfigureConquerorDoesNotAddFeatureProvidersAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            _ = services.ConfigureConqueror();
            _ = services.ConfigureConqueror();

            var applicationPartManager = services.Select(d => d.ImplementationInstance).OfType<ApplicationPartManager>().Single();

            Assert.That(applicationPartManager.FeatureProviders.Where(p => p is HttpQueryControllerFeatureProvider).ToList(), Has.Count.EqualTo(1));
        }
    }
}
