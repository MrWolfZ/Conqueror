using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [TestFixture]
    public class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConqueror();

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(AspNetCoreServerServiceCollectionConfigurator)));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorRegistered_ConfigureConquerorAddsFeatureProviders()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConqueror();

            _ = services.ConfigureConqueror();

            var applicationPartManager = services.Select(d => d.ImplementationInstance).OfType<ApplicationPartManager>().Single();

            Assert.IsNotNull(applicationPartManager.FeatureProviders.SingleOrDefault(p => p is HttpQueryControllerFeatureProvider));
            Assert.IsNotNull(applicationPartManager.FeatureProviders.SingleOrDefault(p => p is HttpCommandControllerFeatureProvider));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyConfigured_ConfigureConquerorDoesNotAddFeatureProvidersAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConqueror();

            _ = services.ConfigureConqueror();
            _ = services.ConfigureConqueror();

            var applicationPartManager = services.Select(d => d.ImplementationInstance).OfType<ApplicationPartManager>().Single();

            Assert.That(applicationPartManager.FeatureProviders.Where(p => p is HttpQueryControllerFeatureProvider).ToList(), Has.Count.EqualTo(1));
            Assert.That(applicationPartManager.FeatureProviders.Where(p => p is HttpCommandControllerFeatureProvider).ToList(), Has.Count.EqualTo(1));
        }
    }
}
