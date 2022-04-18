using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    public sealed class RegistrationTests
    {
        [Test]
        public void GivenAlreadyRegisteredDefaultOptionsWhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients().ConfigureDefaultHttpClientOptions(_ => { });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorHttpClients().ConfigureDefaultHttpClientOptions(_ => { }));
        }
    }
}
