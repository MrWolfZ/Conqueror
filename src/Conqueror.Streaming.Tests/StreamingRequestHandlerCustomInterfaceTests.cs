using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class StreamingRequestHandlerCustomInterfaceTests
{
    [Test]
    public async Task GivenStreamingRequest_HandlerReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestStreamingRequestHandler>();

        var request = new TestStreamingRequest();

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

        var handler = provider.GetRequiredService<IGenericTestStreamingRequestHandler<string>>();

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

        var handler = provider.GetRequiredService<ITestStreamingRequestHandler>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new(), tokenSource.Token).Drain();

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

        var handler = provider.GetRequiredService<ITestStreamingRequestHandler>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new()).WithCancellation(tokenSource.Token).Drain();

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

        var handler = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await handler.ExecuteRequest(new()).Drain();

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

        var handler = provider.GetRequiredService<ITestStreamingRequestHandler>();

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

        var handler = provider.GetRequiredService<IThrowingStreamingRequestHandler>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler>());
    }

    [Test]
    public async Task GivenHandlerWithCustomInterface_ResolvingHandlerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var plainInterfaceHandler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var customInterfaceHandler = provider.GetRequiredService<ITestStreamingRequestHandler>();

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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler2>());
    }

    [Test]
    public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterfaceWithExtraMethod>());
    }

    public sealed record TestStreamingRequest(int Payload = 0);

    public sealed record TestItem(int Payload);

    public sealed record TestStreamingRequest2;

    public sealed record TestItem2;

    public sealed record GenericTestStreamingRequest<T>(T Payload);

    public sealed record GenericTestItem<T>(T Payload);

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
    }

    public interface ITestStreamingRequestHandler2 : IStreamingRequestHandler<TestStreamingRequest2, TestItem2>
    {
    }

    public interface IGenericTestStreamingRequestHandler<T> : IStreamingRequestHandler<GenericTestStreamingRequest<T>, GenericTestItem<T>>
    {
    }

    public interface IThrowingStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
    }

    public interface ITestStreamingRequestHandlerWithExtraMethod : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        void ExtraMethod();
    }

    private sealed class TestStreamingRequestHandler : ITestStreamingRequestHandler
    {
        private readonly TestObservations observations;

        public TestStreamingRequestHandler(TestObservations observations)
        {
            this.observations = observations;
        }

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

    private sealed class TestStreamingRequestHandlerWithMultipleInterfaces : ITestStreamingRequestHandler,
                                                                             ITestStreamingRequestHandler2
    {
        private readonly TestObservations observations;

        public TestStreamingRequestHandlerWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

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

    private sealed class GenericTestStreamingRequestHandler<T> : IGenericTestStreamingRequestHandler<T>
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

    private sealed class ThrowingStreamingRequestHandler : IThrowingStreamingRequestHandler
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

    private sealed class TestStreamingRequestHandlerWithCustomInterfaceWithExtraMethod : ITestStreamingRequestHandlerWithExtraMethod
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = new();

        public List<object> Requests { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}
