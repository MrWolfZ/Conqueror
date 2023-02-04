using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CqsHttpServerAspNetCoreRegistrationFinalizer)));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorRegistered_FinalizeConquerorRegistrationsAddsEndpointFeatureProvider()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers().AddConquerorCQSHttpControllers();

            _ = services.FinalizeConquerorRegistrations();

            var applicationPartManager = services.Select(d => d.ImplementationInstance).OfType<ApplicationPartManager>().Single();

            Assert.IsNotNull(applicationPartManager.FeatureProviders.SingleOrDefault(p => p is HttpEndpointControllerFeatureProvider));
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateCommandName_FinalizeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers()
                        .AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<DuplicateCommandName.TestCommandHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateQueryName_FinalizeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers()
                        .AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<DuplicateQueryName.TestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateCommandPathFromConvention_FinalizeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers()
                        .AddConquerorCQSHttpControllers(o => o.CommandPathConvention = new HttpCommandPathConventionWithDuplicates());

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommand2Handler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateQueryPathFromConvention_FinalizeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers()
                        .AddConquerorCQSHttpControllers(o => o.QueryPathConvention = new HttpQueryPathConventionWithDuplicates());

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQuery2Handler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithDuplicateCommandAndQueryPathFromConvention_FinalizeThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();

            _ = services.AddControllers()
                        .AddConquerorCQSHttpControllers(o =>
                        {
                            o.CommandPathConvention = new HttpCommandPathConventionWithDuplicates();
                            o.QueryPathConvention = new HttpQueryPathConventionWithDuplicates();
                        });

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithCQSControllerRegistrationWithoutFinalization_ThrowsExceptionWhenBuildingServiceProviderWithValidation()
        {
            var services = new ServiceCollection();

            _ = services.AddLogging()
                        .AddSingleton<IHostEnvironment>(_ => throw new())
                        .AddSingleton<IWebHostEnvironment>(_ => throw new())
                        .AddSingleton<DiagnosticListener>(_ => throw new())
                        .AddControllers()
                        .AddConquerorCQSHttpControllers();

            // remove some service registrations that fail validation
            _ = services.RemoveAll<IActionInvokerProvider>();

            var ex = Assert.Throws<AggregateException>(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

            Assert.IsInstanceOf<InvalidOperationException>(ex?.InnerException);
            Assert.That(ex?.InnerException?.Message, Contains.Substring("DidYouForgetToCallFinalizeConquerorRegistrations"));
        }

        [Test]
        public void GivenServiceCollectionWithCQSControllerRegistrationWithFinalizationAlreadyDone_ThrowsExceptionWhenCallingFinalizationAgain()
        {
            var services = new ServiceCollection();

            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithFinalizationAlreadyDone_ThrowsExceptionWhenRegisteringCQS()
        {
            var services = new ServiceCollection().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddMvc().AddConquerorCQSHttpControllers());
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
