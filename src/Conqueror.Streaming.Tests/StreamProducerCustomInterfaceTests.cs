using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamProducerCustomInterfaceTests
{
    [Test]
    public async Task GivenStreamingRequest_ProducerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<ITestStreamProducer>();

        var request = new TestStreamingRequest();

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenGenericStreamingRequest_ProducerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<GenericTestStreamProducer<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IGenericTestStreamProducer<string>>();

        var request = new GenericTestStreamingRequest<string>("test string");

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenCancellationToken_ProducerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<ITestStreamProducer>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new(), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenViaAsyncEnumerableExtension_ProducerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<ITestStreamProducer>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new()).WithCancellation(tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_ProducerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<ITestStreamProducer>();

        _ = await producer.ExecuteRequest(new()).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenStreamingRequest_ProducerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<ITestStreamProducer>();

        var request = new TestStreamingRequest(10);

        var response = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(response, Is.EquivalentTo(new[] { new TestItem(request.Payload + 1), new TestItem(request.Payload + 2), new TestItem(request.Payload + 3) }));
    }

    [Test]
    public void GivenExceptionInProducer_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorStreamProducer<ThrowingStreamProducer>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IThrowingStreamProducer>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => producer.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenProducerWithCustomInterface_ProducerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenProducerWithCustomInterface_ProducerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer>());
    }

    [Test]
    public async Task GivenProducerWithCustomInterface_ResolvingProducerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var plainInterfaceProducer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var customInterfaceProducer = provider.GetRequiredService<ITestStreamProducer>();

        _ = await plainInterfaceProducer.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await customInterfaceProducer.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public void GivenProducerWithMultipleCustomInterfaces_ProducerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer2>());
    }

    [Test]
    public void GivenProducerWithCustomInterfaceWithExtraMethods_RegisteringProducerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamProducer<TestStreamProducerWithCustomInterfaceWithExtraMethod>());
    }

    public sealed record TestStreamingRequest(int Payload = 0);

    public sealed record TestItem(int Payload);

    public sealed record TestStreamingRequest2;

    public sealed record TestItem2;

    public sealed record GenericTestStreamingRequest<T>(T Payload);

    public sealed record GenericTestItem<T>(T Payload);

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>;

    public interface ITestStreamProducer2 : IStreamProducer<TestStreamingRequest2, TestItem2>;

    public interface IGenericTestStreamProducer<T> : IStreamProducer<GenericTestStreamingRequest<T>, GenericTestItem<T>>;

    public interface IThrowingStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>;

    public interface ITestStreamProducerWithExtraMethod : IStreamProducer<TestStreamingRequest, TestItem>
    {
        void ExtraMethod();
    }

    private sealed class TestStreamProducer(TestObservations observations) : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Requests.Add(request);
            observations.CancellationTokens.Add(cancellationToken);
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    private sealed class TestStreamProducerWithMultipleInterfaces(TestObservations observations) : ITestStreamProducer,
                                                                                                   ITestStreamProducer2
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Requests.Add(request);
            observations.CancellationTokens.Add(cancellationToken);
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }

        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Requests.Add(request);
            observations.CancellationTokens.Add(cancellationToken);
            yield return new();
        }
    }

    private sealed class GenericTestStreamProducer<T>(TestObservations observations) : IGenericTestStreamProducer<T>
    {
        public async IAsyncEnumerable<GenericTestItem<T>> ExecuteRequest(GenericTestStreamingRequest<T> request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Requests.Add(request);
            observations.CancellationTokens.Add(cancellationToken);
            yield return new(request.Payload);
            yield return new(request.Payload);
            yield return new(request.Payload);
        }
    }

    private sealed class ThrowingStreamProducer(Exception exception) : IThrowingStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (request != null)
            {
                throw exception;
            }

            yield break;
        }
    }

    private sealed class TestStreamProducerWithCustomInterfaceWithExtraMethod : ITestStreamProducerWithExtraMethod
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = [];

        public List<object> Requests { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
