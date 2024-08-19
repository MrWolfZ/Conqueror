using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamProducerFunctionalityTests
{
    [Test]
    public async Task GivenStreamingRequest_ProducerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenStreamingRequest_DelegateProducerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

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

        var producer = provider.GetRequiredService<IStreamProducer<GenericTestStreamingRequest<string>, GenericTestItem<string>>>();

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

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new(10), tokenSource.Token).Drain();

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

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new(10)).WithCancellation(tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationToken_DelegateProducerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new(10), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenViaAsyncEnumerableExtension_DelegateProducerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new(10)).WithCancellation(tokenSource.Token).Drain();

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

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(10)).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenNoCancellationToken_DelegateProducerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(10)).Drain();

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

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        var response = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(response, Is.EquivalentTo(new[] { new TestItem(request.Payload + 1), new TestItem(request.Payload + 2), new TestItem(request.Payload + 3) }));
    }

    [Test]
    public async Task GivenStreamingRequest_DelegateProducerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((request, _, _) => AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1),
                                                                                                                                    new TestItem(request.Payload + 2),
                                                                                                                                    new TestItem(request.Payload + 3)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

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

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => producer.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenProducerWithInvalidInterface_RegisteringProducerThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamProducer(new TestStreamProducerWithoutValidInterfaces()));
    }

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed record GenericTestStreamingRequest<T>(T Payload);

    private sealed record GenericTestItem<T>(T Payload);

    private sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        private readonly TestObservations observations;

        public TestStreamProducer(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Requests.Add(request);
            observations.CancellationTokens.Add(cancellationToken);
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    private sealed class GenericTestStreamProducer<T> : IStreamProducer<GenericTestStreamingRequest<T>, GenericTestItem<T>>
    {
        private readonly TestObservations observations;

        public GenericTestStreamProducer(TestObservations observations)
        {
            this.observations = observations;
        }

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

    private sealed class ThrowingStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        private readonly Exception exception;

        public ThrowingStreamProducer(Exception exception)
        {
            this.exception = exception;
        }

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

    private sealed class TestStreamProducerWithoutValidInterfaces : IStreamProducer
    {
    }

    private sealed class TestObservations
    {
        public List<object> Requests { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}
