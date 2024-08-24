namespace Conqueror.Streaming.Tests;

[TestFixture]
public sealed class StreamConsumerMiddlewareServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredMiddlewareType_AddingIdenticalMiddlewareDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>();

        Assert.DoesNotThrow(() => services.AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>());
    }

    [Test]
    public void GivenRegisteredMiddlewareType_AddingIdenticalMiddlewareOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                                              .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamConsumerMiddleware)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredMiddlewareType_ItCanBeResolvedDirectly()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>();

        using var serviceProvider = services.BuildServiceProvider();

        // ReSharper disable once AccessToDisposedClosure
        Assert.That(() => serviceProvider.GetRequiredService<TestStreamConsumerMiddleware>(), Throws.Nothing);
    }

    private sealed class TestStreamConsumerMiddleware : IStreamConsumerMiddleware
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            return ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }
}
