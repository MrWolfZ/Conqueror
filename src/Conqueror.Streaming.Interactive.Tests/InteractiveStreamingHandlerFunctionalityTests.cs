using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Interactive.Tests
{
    public sealed class InteractiveStreamingHandlerFunctionalityTests
    {
        [Test]
        public async Task GivenRequest_HandlerReceivesRequest()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>();

            var request = new TestRequest(10);

            _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

            Assert.That(observations.Queries, Is.EquivalentTo(new[] { request }));
        }

        [Test]
        public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteRequest(new(10), tokenSource.Token).Drain();

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenCancellationTokenViaAsyncEnumerableExtension_HandlerReceivesCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>();
            using var tokenSource = new CancellationTokenSource();

            var enumerator = handler.ExecuteRequest(new(10), CancellationToken.None).WithCancellation(tokenSource.Token).GetAsyncEnumerator();

            _ = await enumerator.MoveNextAsync();

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenRequest_HandlerReturnsStream()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>();

            var request = new TestRequest(10);

            var response = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

            Assert.That(response, Has.Count.EqualTo(3));
            Assert.AreEqual(request.Payload + 1, response.ElementAt(0).Payload);
            Assert.AreEqual(request.Payload + 2, response.ElementAt(1).Payload);
            Assert.AreEqual(request.Payload + 3, response.ElementAt(2).Payload);
        }

        [Test]
        public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorInteractiveStreaming().AddTransient<TestStreamingHandlerWithoutValidInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorInteractiveStreaming().AddScoped<TestStreamingHandlerWithoutValidInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorInteractiveStreaming().AddSingleton<TestStreamingHandlerWithoutValidInterfaces>().ConfigureConqueror());
        }

        private sealed record TestRequest(int Payload);

        private sealed record TestItem(int Payload);

        private sealed class TestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
        {
            private readonly TestObservations responses;

            public TestStreamingHandler(TestObservations responses)
            {
                this.responses = responses;
            }

            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                responses.Queries.Add(request);
                responses.CancellationTokens.Add(cancellationToken);
                yield return new(request.Payload + 1);
                yield return new(request.Payload + 2);
                yield return new(request.Payload + 3);
            }
        }

        private sealed class TestStreamingHandlerWithoutValidInterfaces : IInteractiveStreamingHandler
        {
        }

        private sealed class TestObservations
        {
            public List<object> Queries { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
