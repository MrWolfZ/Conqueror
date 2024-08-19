using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers().AddConquerorStreamingHttpControllers();

            Assert.That(services.Count(d => d.ServiceType == typeof(HttpEndpointRegistry)), Is.EqualTo(1));
            Assert.That(services.Count(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)), Is.EqualTo(1));
            Assert.That(services.Count(d => d.ImplementationType == typeof(HttpEndpointConfigurationStartupFilter)), Is.EqualTo(1));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateStreamName_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorStreamingHttpControllers();

                    _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                                .AddConquerorStreamProducer<DuplicateStreamName.TestStreamProducer>();
                });

                _ = webHost.Configure(app =>
                {
                    _ = app.UseRouting();
                    _ = app.UseConqueror();
                    _ = app.UseEndpoints(b => b.MapControllers());
                });
            });

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => hostBuilder.StartAsync());
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateStreamNameFromDelegate_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorStreamingHttpControllers();

                    _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                                .AddConquerorStreamProducerDelegate<DuplicateStreamName.TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()));
                });

                _ = webHost.Configure(app =>
                {
                    _ = app.UseRouting();
                    _ = app.UseConqueror();
                    _ = app.UseEndpoints(b => b.MapControllers());
                });
            });

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => hostBuilder.StartAsync());
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateStreamPathFromConvention_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorStreamingHttpControllers(o => o.PathConvention = new HttpStreamPathConventionWithDuplicates());

                    _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                                .AddConquerorStreamProducer<TestStreamingRequest2Handler>();
                });

                _ = webHost.Configure(app =>
                {
                    _ = app.UseRouting();
                    _ = app.UseConqueror();
                    _ = app.UseEndpoints(b => b.MapControllers());
                });
            });

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => hostBuilder.StartAsync());
        }

        [HttpStream]
        public sealed record TestStreamingRequest;

        public sealed record TestItem;

        [HttpStream]
        public sealed record TestStreamingRequest2;

        public sealed record TestItem2;

        public sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new();
            }
        }

        public sealed class TestStreamingRequest2Handler : IStreamProducer<TestStreamingRequest2, TestItem2>
        {
            public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new();
            }
        }

        private sealed class HttpStreamPathConventionWithDuplicates : IHttpStreamPathConvention
        {
            public string GetStreamPath(Type requestType, HttpStreamAttribute attribute)
            {
                return "/duplicate";
            }
        }
    }

#pragma warning disable SA1403 // okay for testing purposes

    namespace DuplicateStreamName
    {
        [HttpStream]
        public sealed record TestStreamingRequest;

        public sealed record TestItem;

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "okay for testing purposes")]
        public sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new();
            }
        }
    }
}
