using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public class ApiDescriptionTests : TestBase
{
    private IApiDescriptionGroupCollectionProvider ApiDescriptionProvider => Resolve<IApiDescriptionGroupCollectionProvider>();

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptors()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequest).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/streams/Test"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithoutPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequestWithoutPayload).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/streams/TestStreamingRequestWithoutPayload"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithComplexPayload()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequestWithComplexPayload).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/streams/TestStreamingRequestWithComplexPayload"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithCustomPathConvention()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequest3).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/streams/TestStreamingRequest3FromConvention"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithCustomPath()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequestWithCustomPath).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/testStreamingRequestWithCustomPath"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithVersion()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequestWithVersion).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/v2/streams/TestStreamingRequestWithVersion"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithOperationId()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "custom-stream-op-id");

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/streams/TestStreamingRequestWithOperationId"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.Null);
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsStreamDescriptorsWithApiGroupName()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == typeof(TestStreamingRequestWithApiGroupName).FullName);

        Assert.That(queryApiDescription, Is.Not.Null);
        Assert.That(queryApiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(queryApiDescription?.RelativePath, Is.EqualTo("api/streams/TestStreamingRequestWithApiGroupName"));
        Assert.That(queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(queryApiDescription?.GroupName, Is.EqualTo("Custom Stream Group"));
        Assert.That(queryApiDescription?.ParameterDescriptions.Count, Is.EqualTo(0));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorStreamingHttpControllers(o => { o.PathConvention = new TestHttpStreamPathConvention(); });

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamingRequestHandler2>()
                    .AddConquerorStreamProducer<TestStreamingRequestHandler3>()
                    .AddConquerorStreamProducer<TestStreamProducerWithoutPayload>()
                    .AddConquerorStreamProducer<TestStreamProducerWithComplexPayload>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithCustomPathHandler>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithVersionHandler>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithOperationIdHandler>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithApiGroupNameHandler>();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpStream]
    public sealed record TestStreamingRequest
    {
        public int Payload { get; init; }
    }

    public sealed record TestItem
    {
        public int Payload { get; init; }
    }

    [HttpStream]
    public sealed record TestStreamingRequest2;

    public sealed record TestItem2;

    [HttpStream]
    public sealed record TestStreamingRequest3
    {
        public int Payload { get; init; }
    }

    [HttpStream]
    public sealed record TestStreamingRequestWithoutPayload;

    [HttpStream]
    public sealed record TestStreamingRequestWithComplexPayload(TestStreamingRequestWithComplexPayloadPayload Payload);

    public sealed record TestStreamingRequestWithComplexPayloadPayload(int Payload);

    [HttpStream(Path = "/api/testStreamingRequestWithCustomPath")]
    public sealed record TestStreamingRequestWithCustomPath
    {
        public int Payload { get; init; }
    }

    [HttpStream(Version = "v2")]
    public sealed record TestStreamingRequestWithVersion
    {
        public int Payload { get; init; }
    }

    [HttpStream(OperationId = "custom-stream-op-id")]
    public sealed record TestStreamingRequestWithOperationId
    {
        public int Payload { get; init; }
    }

    [HttpStream(ApiGroupName = "Custom Stream Group")]
    public sealed record TestStreamingRequestWithApiGroupName
    {
        public int Payload { get; init; }
    }

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>;

    public sealed class TestStreamProducer : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload + 1 };
        }
    }

    public sealed class TestStreamingRequestHandler2 : IStreamProducer<TestStreamingRequest2, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestStreamingRequestHandler3 : IStreamProducer<TestStreamingRequest3, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest3 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestStreamProducerWithoutPayload : IStreamProducer<TestStreamingRequestWithoutPayload, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = 11 };
        }
    }

    public sealed class TestStreamProducerWithComplexPayload : IStreamProducer<TestStreamingRequestWithComplexPayload, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithComplexPayload request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload.Payload + 1 };
        }
    }

    public sealed class TestStreamingRequestWithCustomPathHandler : IStreamProducer<TestStreamingRequestWithCustomPath, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithCustomPath request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload + 1 };
        }
    }

    public sealed class TestStreamingRequestWithVersionHandler : IStreamProducer<TestStreamingRequestWithVersion, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithVersion request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload + 1 };
        }
    }

    public sealed class TestStreamingRequestWithOperationIdHandler : IStreamProducer<TestStreamingRequestWithOperationId, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithOperationId request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload + 1 };
        }
    }

    public sealed class TestStreamingRequestWithApiGroupNameHandler : IStreamProducer<TestStreamingRequestWithApiGroupName, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithApiGroupName request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload + 1 };
        }
    }

    private sealed class TestHttpStreamPathConvention : IHttpStreamPathConvention
    {
        public string? GetStreamPath(Type requestType, HttpStreamAttribute attribute)
        {
            if (requestType != typeof(TestStreamingRequest3))
            {
                return null;
            }

            return $"/api/streams/{requestType.Name}FromConvention";
        }
    }
}
