using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    public class ApiDescriptionTests : TestBase
    {
        private IApiDescriptionGroupCollectionProvider ApiDescriptionProvider => Resolve<IApiDescriptionGroupCollectionProvider>();

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptors()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommand).FullName);

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(200, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(1, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutResponse()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithoutResponse).FullName);

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(204, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(1, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithoutPayload).FullName);

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(200, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(0, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutResponseWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithoutResponseWithoutPayload).FullName);

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(204, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(0, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsQueryDescriptors()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQuery).FullName);

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Get, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(1, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsPostQueryDescriptors()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQuery).FullName);

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Post, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(1, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithoutPayload).FullName);

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Get, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(0, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithComplexPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithComplexPayload).FullName);

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Get, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(1, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQueryWithoutPayload).FullName);

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Post, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(0, queryApiDescription?.ParameterDescriptions.Count);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandHandlerWithoutPayload>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutPayload>();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestQueryHandlerWithComplexPayload>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestPostQueryHandlerWithoutPayload>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034
        
        [HttpQuery]
        public sealed record TestQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestQueryResponse
        {
            public int Payload { get; init; }
        }
        
        [HttpQuery]
        public sealed record TestQuery2;

        public sealed record TestQueryResponse2;
    
        [HttpQuery]
        public sealed record TestQueryWithoutPayload;

        [HttpQuery]
        public sealed record TestQueryWithComplexPayload(TestQueryWithComplexPayloadPayload Payload);

        public sealed record TestQueryWithComplexPayloadPayload(int Payload);
    
        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery
        {
            public int Payload { get; init; }
        }
    
        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithoutPayload;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
        {
        }

        public sealed class TestQueryHandler : ITestQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
        {
            public Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestQueryHandlerWithComplexPayload : IQueryHandler<TestQueryWithComplexPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithComplexPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload.Payload + 1 };
            }
        }

        public sealed class TestPostQueryHandler : ITestPostQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestPostQueryHandlerWithoutPayload : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }
        
        [HttpCommand]
        public sealed record TestCommand
        {
            public int Payload { get; init; }
        }

        public sealed record TestCommandResponse
        {
            public int Payload { get; init; }
        }
        
        [HttpCommand]
        public sealed record TestCommand2;

        public sealed record TestCommandResponse2;
        
        [HttpCommand]
        public sealed record TestCommandWithoutPayload;

        [HttpCommand]
        public sealed record TestCommandWithoutResponse
        {
            public int Payload { get; init; }
        }
    
        [HttpCommand]
        public sealed record TestCommandWithoutResponseWithoutPayload;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        public sealed class TestCommandHandler : ITestCommandHandler
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = command.Payload + 1 };
            }
        }

        public sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
        {
            public Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
        {
            public async Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
