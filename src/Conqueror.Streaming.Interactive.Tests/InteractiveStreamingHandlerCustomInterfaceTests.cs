using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Interactive.Tests
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation and assembly scanning to work")]
    public sealed class InteractiveStreamingHandlerCustomInterfaceTests
    {
        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>());
        }

        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
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

            _ = services.AddConquerorInteractiveStreaming()
                        .AddSingleton<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var plainInterfaceHandler = provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>();
            var customInterfaceHandler = provider.GetRequiredService<ITestStreamingHandler>();

            _ = await plainInterfaceHandler.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await customInterfaceHandler.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.AreEqual(2, observations.Instances.Count);
            Assert.AreSame(observations.Instances[0], observations.Instances[1]);
        }

        [Test]
        public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingHandler>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest2, TestItem2>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingHandler2>());
        }

        [Test]
        public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorInteractiveStreaming().AddTransient<TestStreamingHandlerWithCustomInterfaceWithExtraMethod>().FinalizeConquerorRegistrations());
        }

        public sealed record TestRequest;

        public sealed record TestItem;

        public sealed record TestRequest2;

        public sealed record TestItem2;

        public interface ITestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
        {
        }

        public interface ITestStreamingHandler2 : IInteractiveStreamingHandler<TestRequest2, TestItem2>
        {
        }

        public interface ITestStreamingHandlerWithExtraMethod : IInteractiveStreamingHandler<TestRequest, TestItem>
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
}
