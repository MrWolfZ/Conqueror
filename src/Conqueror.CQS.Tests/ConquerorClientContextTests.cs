using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class ConquerorClientContextTests
    {
        [Test]
        public void GivenExistingClientContext_ActivatingContextAgainThrowsException()
        {
            var provider = new ServiceCollection().AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            var clientContext = provider.GetRequiredService<IConquerorClientContext>();

            _ = clientContext.Activate();

            _ = Assert.Throws<InvalidOperationException>(() => clientContext.Activate());
        }

        [Test]
        public void GivenInactiveClientContext_IsActiveReturnsFalse()
        {
            var provider = new ServiceCollection().AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            var clientContext = provider.GetRequiredService<IConquerorClientContext>();

            Assert.IsFalse(clientContext.IsActive);
        }

        [Test]
        public void GivenActiveClientContext_IsActiveReturnsTrue()
        {
            var provider = new ServiceCollection().AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            var clientContext = provider.GetRequiredService<IConquerorClientContext>();

            _ = clientContext.Activate();

            Assert.IsTrue(clientContext.IsActive);
        }

        [Test]
        public void GivenInactiveClientContext_GettingContextItemsThrowsException()
        {
            var provider = new ServiceCollection().AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            var clientContext = provider.GetRequiredService<IConquerorClientContext>();

            _ = Assert.Throws<InvalidOperationException>(() => clientContext.GetItems());
        }

        [Test]
        public void GivenActiveClientContext_GettingItemsReturnsSameResultAsActivation()
        {
            var provider = new ServiceCollection().AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            var clientContext = provider.GetRequiredService<IConquerorClientContext>();

            var itemsFromActivation = clientContext.Activate();

            Assert.AreSame(itemsFromActivation, clientContext.GetItems());
        }
    }
}
