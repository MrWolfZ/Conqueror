using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation and assembly scanning to work")]
public sealed class StreamingHandlerCustomInterfaceTests
{
    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingHandler>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>());
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingHandler>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingHandler>());
    }

    [Test]
    public async Task GivenHandlerWithCustomInterface_ResolvingHandlerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddSingleton<TestStreamingHandler>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var plainInterfaceHandler = provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>();
        var customInterfaceHandler = provider.GetRequiredService<ITestStreamingHandler>();

        _ = await plainInterfaceHandler.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await customInterfaceHandler.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingHandlerWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingHandler>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest2, TestItem2>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingHandler2>());
    }

    [Test]
    public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreaming().AddTransient<TestStreamingHandlerWithCustomInterfaceWithExtraMethod>().FinalizeConquerorRegistrations());
    }

    public sealed record TestRequest;

    public sealed record TestItem;

    public sealed record TestRequest2;

    public sealed record TestItem2;

    public interface ITestStreamingHandler : IStreamingHandler<TestRequest, TestItem>
    {
    }

    public interface ITestStreamingHandler2 : IStreamingHandler<TestRequest2, TestItem2>
    {
    }

    public interface ITestStreamingHandlerWithExtraMethod : IStreamingHandler<TestRequest, TestItem>
    {
        void ExtraMethod();
    }

    private sealed class TestStreamingHandler : ITestStreamingHandler
    {
        private readonly TestObservations observations;

        public TestStreamingHandler(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            yield break;
        }
    }

    private sealed class TestStreamingHandlerWithMultipleInterfaces : ITestStreamingHandler,
                                                                      ITestStreamingHandler2
    {
        private readonly TestObservations observations;

        public TestStreamingHandlerWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            yield break;
        }

        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            yield break;
        }
    }

    private sealed class TestStreamingHandlerWithCustomInterfaceWithExtraMethod : ITestStreamingHandlerWithExtraMethod
    {
        public void ExtraMethod() => throw new NotSupportedException();

        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = new();
    }
}
