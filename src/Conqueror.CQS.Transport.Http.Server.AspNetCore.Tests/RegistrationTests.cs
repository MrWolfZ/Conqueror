using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers().AddConquerorCQSHttpControllers();

            Assert.That(services.Count(d => d.ServiceType == typeof(HttpEndpointRegistry)), Is.EqualTo(1));
            Assert.That(services.Count(d => d.ServiceType == typeof(HttpEndpointActionDescriptorChangeProvider)), Is.EqualTo(1));
            Assert.That(services.Count(d => d.ImplementationType == typeof(HttpEndpointConfigurationStartupFilter)), Is.EqualTo(1));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateCommandName_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers();

                    _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                                .AddConquerorCommandHandler<DuplicateCommandName.TestCommandHandler>();
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
        public void GivenServiceCollectionWithDuplicateCommandNameFromDelegate_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers();

                    _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                                .AddConquerorCommandHandlerDelegate<DuplicateCommandName.TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()));
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
        public void GivenServiceCollectionWithDuplicateQueryName_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers();

                    _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                                .AddConquerorQueryHandler<DuplicateQueryName.TestQueryHandler>();
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
        public void GivenServiceCollectionWithDuplicateQueryNameFromDelegate_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers();

                    _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                                .AddConquerorQueryHandlerDelegate<DuplicateQueryName.TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()));
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
        public void GivenServiceCollectionWithDuplicateCommandPathFromConvention_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new HttpCommandPathConventionWithDuplicates());

                    _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                                .AddConquerorCommandHandler<TestCommand2Handler>();
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
        public void GivenServiceCollectionWithDuplicateQueryPathFromConvention_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers(o => o.QueryPathConvention = new HttpQueryPathConventionWithDuplicates());

                    _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                                .AddConquerorQueryHandler<TestQuery2Handler>();
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
        public void GivenServiceCollectionWithDuplicateCommandAndQueryPathFromConvention_StartingHostThrowsInvalidOperationException()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(services =>
                {
                    _ = services.AddControllers()
                                .AddConquerorCQSHttpControllers(o =>
                                {
                                    o.CommandPathConvention = new HttpCommandPathConventionWithDuplicates();
                                    o.QueryPathConvention = new HttpQueryPathConventionWithDuplicates();
                                });

                    _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                                .AddConquerorQueryHandler<TestQueryHandler>();
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

        [HttpCommand]
        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        [HttpCommand]
        public sealed record TestCommand2;

        public sealed record TestCommand2Response;

        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }

        public sealed class TestCommand2Handler : ICommandHandler<TestCommand2, TestCommand2Response>
        {
            public async Task<TestCommand2Response> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }

        [HttpQuery]
        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        [HttpQuery]
        public sealed record TestQuery2;

        public sealed record TestQuery2Response;

        public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }

        public sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
        {
            public async Task<TestQuery2Response> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }

        private sealed class HttpCommandPathConventionWithDuplicates : IHttpCommandPathConvention
        {
            public string GetCommandPath(Type commandType, HttpCommandAttribute attribute)
            {
                return "/duplicate";
            }
        }

        private sealed class HttpQueryPathConventionWithDuplicates : IHttpQueryPathConvention
        {
            public string GetQueryPath(Type queryType, HttpQueryAttribute attribute)
            {
                return "/duplicate";
            }
        }
    }

#pragma warning disable SA1403 // okay for testing purposes

    namespace DuplicateQueryName
    {
        [HttpQuery]
        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "okay for testing purposes")]
        public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }
    }

    namespace DuplicateCommandName
    {
        [HttpCommand]
        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "okay for testing purposes")]
        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new();
            }
        }
    }
}
