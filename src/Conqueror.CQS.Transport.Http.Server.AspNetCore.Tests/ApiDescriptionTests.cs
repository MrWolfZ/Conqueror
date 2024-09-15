using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
public class ApiDescriptionTests : TestBase
{
    private IApiDescriptionGroupCollectionProvider ApiDescriptionProvider => Resolve<IApiDescriptionGroupCollectionProvider>();

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptors()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommand).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/Test"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutResponse()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithoutResponse).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/TestCommandWithoutResponse"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithoutPayload).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/TestCommandWithoutPayload"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutResponseWithoutPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithoutResponseWithoutPayload).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/TestCommandWithoutResponseWithoutPayload"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithCustomPathConvention()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommand3).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/TestCommand3FromConvention"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithCustomPath()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithCustomPath).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/testCommandWithCustomPath"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithVersion()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithVersion).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/v2/commands/TestCommandWithVersion"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithOperationId()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "custom-command-op-id");

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/TestCommandWithOperationId"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.Null);
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithApiGroupName()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestCommandWithApiGroupName).FullName);

        Assert.That(commandApiDescription, Is.Not.Null);
        Assert.That(commandApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(commandApiDescription?.RelativePath, Is.EqualTo("api/commands/TestCommandWithApiGroupName"));
        Assert.That(commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(commandApiDescription?.GroupName, Is.EqualTo("Custom Command Group"));
        Assert.That(commandApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptors()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQuery).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/Test"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptors()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQuery).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestPost"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithoutPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithoutPayload).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestQueryWithoutPayload"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithComplexPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithComplexPayload).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestQueryWithComplexPayload"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithoutPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQueryWithoutPayload).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestPostQueryWithoutPayload"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithCustomPathConvention()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQuery3).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestQuery3FromConvention"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithCustomPathConvention()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQuery2).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestPostQuery2FromConvention"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithCustomPath()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithCustomPath).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/testQueryWithCustomPath"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithCustomPath()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQueryWithCustomPath).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/testPostQueryWithCustomPath"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithVersion()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithVersion).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/v2/queries/TestQueryWithVersion"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithVersion()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQueryWithVersion).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/v2/queries/TestPostQueryWithVersion"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithOperationId()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "custom-query-op-id");

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestQueryWithOperationId"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithOperationId()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "custom-post-query-op-id");

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestPostQueryWithOperationId"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithApiGroupName()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestQueryWithApiGroupName).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestQueryWithApiGroupName"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.EqualTo("Custom Query Group"));
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithApiGroupName()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestPostQueryWithApiGroupName).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Post));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/queries/TestPostQueryWithApiGroupName"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.EqualTo("Custom POST Query Group"));
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers(o =>
        {
            o.CommandPathConvention = new TestHttpCommandPathConvention();
            o.QueryPathConvention = new TestHttpQueryPathConvention();
        });

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandHandler2>()
                    .AddConquerorCommandHandler<TestCommandHandler3>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutPayload>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutPayload>()
                    .AddConquerorCommandHandler<TestCommandWithCustomPathHandler>()
                    .AddConquerorCommandHandler<TestCommandWithVersionHandler>()
                    .AddConquerorCommandHandler<TestCommandWithOperationIdHandler>()
                    .AddConquerorCommandHandler<TestCommandWithApiGroupNameHandler>();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestQueryHandler2>()
                    .AddConquerorQueryHandler<TestQueryHandler3>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithoutPayload>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithComplexPayload>()
                    .AddConquerorQueryHandler<TestQueryWithCustomPathHandler>()
                    .AddConquerorQueryHandler<TestQueryWithVersionHandler>()
                    .AddConquerorQueryHandler<TestQueryWithOperationIdHandler>()
                    .AddConquerorQueryHandler<TestQueryWithApiGroupNameHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler2>()
                    .AddConquerorQueryHandler<TestPostQueryHandlerWithoutPayload>()
                    .AddConquerorQueryHandler<TestPostQueryWithCustomPathHandler>()
                    .AddConquerorQueryHandler<TestPostQueryWithVersionHandler>()
                    .AddConquerorQueryHandler<TestPostQueryWithOperationIdHandler>()
                    .AddConquerorQueryHandler<TestPostQueryWithApiGroupNameHandler>();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

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
    public sealed record TestQuery3
    {
        public int Payload { get; init; }
    }

    [HttpQuery]
    public sealed record TestQueryWithoutPayload;

    [HttpQuery]
    public sealed record TestQueryWithComplexPayload(TestQueryWithComplexPayloadPayload Payload);

    public sealed record TestQueryWithComplexPayloadPayload(int Payload);

    [HttpQuery(Path = "/api/testQueryWithCustomPath")]
    public sealed record TestQueryWithCustomPath
    {
        public int Payload { get; init; }
    }

    [HttpQuery(Version = "v2")]
    public sealed record TestQueryWithVersion
    {
        public int Payload { get; init; }
    }

    [HttpQuery(OperationId = "custom-query-op-id")]
    public sealed record TestQueryWithOperationId
    {
        public int Payload { get; init; }
    }

    [HttpQuery(ApiGroupName = "Custom Query Group")]
    public sealed record TestQueryWithApiGroupName
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true)]
    public sealed record TestPostQuery
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true)]
    public sealed record TestPostQuery2
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true)]
    public sealed record TestPostQueryWithoutPayload;

    [HttpQuery(UsePost = true, Path = "/api/testPostQueryWithCustomPath")]
    public sealed record TestPostQueryWithCustomPath
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true, Version = "v2")]
    public sealed record TestPostQueryWithVersion
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true, OperationId = "custom-post-query-op-id")]
    public sealed record TestPostQueryWithOperationId
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true, ApiGroupName = "Custom POST Query Group")]
    public sealed record TestPostQueryWithApiGroupName
    {
        public int Payload { get; init; }
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>;

    public sealed class TestQueryHandler : ITestQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
    {
        public Task<TestQueryResponse2> Handle(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestQueryHandler3 : IQueryHandler<TestQuery3, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery3 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestQueryHandlerWithComplexPayload : IQueryHandler<TestQueryWithComplexPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithComplexPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload.Payload + 1 };
        }
    }

    public sealed class TestQueryWithCustomPathHandler : IQueryHandler<TestQueryWithCustomPath, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithCustomPath query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryWithVersionHandler : IQueryHandler<TestQueryWithVersion, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithVersion query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryWithOperationIdHandler : IQueryHandler<TestQueryWithOperationId, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithOperationId query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestQueryWithApiGroupNameHandler : IQueryHandler<TestQueryWithApiGroupName, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQueryWithApiGroupName query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryHandler : ITestPostQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestPostQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryHandler2 : IQueryHandler<TestPostQuery2, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQuery2 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryHandlerWithoutPayload : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithoutPayload query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestPostQueryWithCustomPathHandler : IQueryHandler<TestPostQueryWithCustomPath, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithCustomPath query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryWithVersionHandler : IQueryHandler<TestPostQueryWithVersion, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithVersion query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryWithOperationIdHandler : IQueryHandler<TestPostQueryWithOperationId, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithOperationId query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
        }
    }

    public sealed class TestPostQueryWithApiGroupNameHandler : IQueryHandler<TestPostQueryWithApiGroupName, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestPostQueryWithApiGroupName query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = query.Payload + 1 };
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
    public sealed record TestCommand3
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestCommandWithoutPayload;

    [HttpCommand]
    public sealed record TestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    [HttpCommand]
    public sealed record TestCommandWithoutResponseWithoutPayload;

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    [HttpCommand(Path = "/api/testCommandWithCustomPath")]
    public sealed record TestCommandWithCustomPath
    {
        public int Payload { get; init; }
    }

    [HttpCommand(Version = "v2")]
    public sealed record TestCommandWithVersion
    {
        public int Payload { get; init; }
    }

    [HttpCommand(OperationId = "custom-command-op-id")]
    public sealed record TestCommandWithOperationId
    {
        public int Payload { get; init; }
    }

    [HttpCommand(ApiGroupName = "Custom Command Group")]
    public sealed record TestCommandWithApiGroupName
    {
        public int Payload { get; init; }
    }

    public sealed class TestCommandHandler : ITestCommandHandler
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
    {
        public Task<TestCommandResponse2> Handle(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestCommandHandler3 : ICommandHandler<TestCommand3, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand3 command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
    {
        public async Task Handle(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public sealed class TestCommandWithCustomPathHandler : ICommandHandler<TestCommandWithCustomPath, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommandWithCustomPath command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithVersionHandler : ICommandHandler<TestCommandWithVersion, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommandWithVersion command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithOperationIdHandler : ICommandHandler<TestCommandWithOperationId, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommandWithOperationId command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    public sealed class TestCommandWithApiGroupNameHandler : ICommandHandler<TestCommandWithApiGroupName, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommandWithApiGroupName command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + 1 };
        }
    }

    private sealed class TestHttpCommandPathConvention : IHttpCommandPathConvention
    {
        public string? GetCommandPath(Type commandType, HttpCommandAttribute attribute)
        {
            if (commandType != typeof(TestCommand3))
            {
                return null;
            }

            return $"/api/commands/{commandType.Name}FromConvention";
        }
    }

    private sealed class TestHttpQueryPathConvention : IHttpQueryPathConvention
    {
        public string? GetQueryPath(Type queryType, HttpQueryAttribute attribute)
        {
            if (queryType != typeof(TestQuery3) && queryType != typeof(TestPostQuery2))
            {
                return null;
            }

            return $"/api/queries/{queryType.Name}FromConvention";
        }
    }
}
