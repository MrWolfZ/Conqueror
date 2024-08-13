using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamingRequestHandlerFunctionalityTests
{
    [Test]
    public async Task GivenStreamingRequest_HandlerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenStreamingRequest_DelegateHandlerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenGenericStreamingRequest_HandlerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<GenericTestStreamingRequestHandler<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<GenericTestStreamingRequest<string>, GenericTestItem<string>>>();

        var request = new GenericTestStreamingRequest<string>("test string");

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new(10), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenViaAsyncEnumerableExtension_HandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new(10)).WithCancellation(tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationToken_DelegateHandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new(10), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenViaAsyncEnumerableExtension_DelegateHandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new(10)).WithCancellation(tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(10)).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenNoCancellationToken_DelegateHandlerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                    {
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Requests.Add(request);
                        obs.CancellationTokens.Add(cancellationToken);
                        return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(10)).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenStreamingRequest_HandlerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        var response = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(response, Is.EquivalentTo(new[] { new TestItem(request.Payload + 1), new TestItem(request.Payload + 2), new TestItem(request.Payload + 3) }));
    }

    [Test]
    public async Task GivenStreamingRequest_DelegateHandlerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((request, _, _) => AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1),
                                                                                                                                             new TestItem(request.Payload + 2),
                                                                                                                                             new TestItem(request.Payload + 3)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        var response = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(response, Is.EquivalentTo(new[] { new TestItem(request.Payload + 1), new TestItem(request.Payload + 2), new TestItem(request.Payload + 3) }));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorStreamingRequestHandler<ThrowingStreamingRequestHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamingRequestHandler(new TestStreamingRequestHandlerWithoutValidInterfaces()));
    }

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed record GenericTestStreamingRequest<T>(T Payload);

    private sealed record GenericTestItem<T>(T Payload);

    private sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        private readonly TestObservations observations;

        public TestStreamingRequestHandler(TestObservations observations)
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

    private sealed class GenericTestStreamingRequestHandler<T> : IStreamingRequestHandler<GenericTestStreamingRequest<T>, GenericTestItem<T>>
    {
        private readonly TestObservations observations;

        public GenericTestStreamingRequestHandler(TestObservations observations)
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

    private sealed class ThrowingStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        private readonly Exception exception;

        public ThrowingStreamingRequestHandler(Exception exception)
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

    private sealed class TestStreamingRequestHandlerWithoutValidInterfaces : IStreamingRequestHandler
    {
    }

    private sealed class TestObservations
    {
        public List<object> Requests { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}
