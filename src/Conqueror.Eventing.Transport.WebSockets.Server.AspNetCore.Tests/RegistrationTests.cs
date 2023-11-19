namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore.Tests;

[TestFixture]
public class RegistrationTests
{
    [Test]
    public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
    {
        var services = new ServiceCollection();

        _ = services.AddControllers().AddConquerorEventingWebSocketsControllers();

        Assert.That(services.Count(d => d.ServiceType == typeof(HttpEndpointRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ImplementationType == typeof(HttpEndpointConfigurationStartupFilter)), Is.EqualTo(1));
    }
}
