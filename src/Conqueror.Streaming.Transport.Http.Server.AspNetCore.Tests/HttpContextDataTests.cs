using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Net.Http.Headers;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[NonParallelizable]
public sealed class HttpContextDataTests : TestBase
{
    private static readonly Dictionary<string, string> ContextData = new()
    {
        { "key1", "value1" },
        { "key2", "value2" },
        { "keyWith,Comma", "value" },
        { "key4", "valueWith,Comma" },
        { "keyWith=Equals", "value" },
        { "key6", "valueWith=Equals" },
        { "keyWith|Pipe", "value" },
        { "key8", "valueWith|Pipe" },
        { "keyWith:Colon", "value" },
        { "key10", "valueWith:Colon" },
    };

    private static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key11", "value1" },
        { "key12", "value2" },
    };

    private DisposableActivity? activity;

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenUpstreamContextData_DataIsReturnedInHeader(string method, string path, string data)
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var response = await ExecuteRequest(method, path, data);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenUpstreamContextDataInAFailedRequest_DataIsReturnedInHeader(string method, string path, string data)
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;
        Resolve<TestObservations>().ExceptionToThrow = new();

        var response = await ExecuteRequest(method, path, data);
        await response.AssertStatusCode(HttpStatusCode.InternalServerError);

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenBidirectionalContextData_DataIsReturnedInHeader(string method, string path, string data)
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        var response = await ExecuteRequest(method, path, data);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.Empty);
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenBidirectionalContextDataInAFailedRequest_DataIsReturnedInHeader(string method, string path, string data)
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;
        Resolve<TestObservations>().ExceptionToThrow = new();

        var response = await ExecuteRequest(method, path, data);
        await response.AssertStatusCode(HttpStatusCode.InternalServerError);

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.Empty);
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenUpstreamAndBidirectionalContextData_DataIsReturnedInHeader(string method, string path, string data)
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var response = await ExecuteRequest(method, path, data);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenUpstreamAndBidirectionalContextDataInAFailedRequest_DataIsReturnedInHeader(string method, string path, string data)
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;
        Resolve<TestObservations>().ShouldAddUpstreamData = true;
        Resolve<TestObservations>().ExceptionToThrow = new();

        var response = await ExecuteRequest(method, path, data);
        await response.AssertStatusCode(HttpStatusCode.InternalServerError);

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorContextRequestHeaderWithDownstreamData_DataIsReceivedByHandler(string method, string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var response = await ExecuteRequest(method, path, data, [(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData())]);

        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;

        Assert.That(receivedContextData?.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorContextRequestHeaderWithBidirectionalData_DataIsReceivedByHandler(string method, string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var response = await ExecuteRequest(method, path, data, [(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData())]);

        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedContextData?.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorContextRequestHeaderWithDownstreamAndBidirectionalData_DataIsReceivedByHandler(string method, string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var response = await ExecuteRequest(method, path, data, [(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData())]);

        await response.AssertSuccessStatusCode();

        var receivedDownstreamContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedDownstreamContextData?.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(receivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenMultipleConquerorContextRequestHeadersWithDownstreamAndBidirectionalData_DataIsReceivedByHandler(string method, string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var encodedData1 = conquerorContext.EncodeDownstreamContextData();

        conquerorContext.DownstreamContextData.Clear();
        conquerorContext.ContextData.Clear();

        conquerorContext.DownstreamContextData.Set("extraKey", "extraValue", ConquerorContextDataScope.AcrossTransports);
        conquerorContext.ContextData.Set("extraKey", "extraValue", ConquerorContextDataScope.AcrossTransports);

        var encodedData2 = conquerorContext.EncodeDownstreamContextData();

        var response = await ExecuteRequest(method, path, data, [
            (HttpConstants.ConquerorContextHeaderName, encodedData1),
            (HttpConstants.ConquerorContextHeaderName, encodedData2),
        ]);

        await response.AssertSuccessStatusCode();

        var receivedDownstreamContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedDownstreamContextData?.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData.Concat([new("extraKey", "extraValue")])));
        Assert.That(receivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData.Concat([new("extraKey", "extraValue")])));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenInvalidConquerorContextRequestHeader_ReturnsBadRequest(string method, string path, string data)
    {
        var response = await ExecuteRequest(method, path, data, [
            (HttpConstants.ConquerorContextHeaderName, "foo=bar"),
        ]);

        await response.AssertStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandler(string method, string path, string data)
    {
        const string expectedTraceId = "80e1a2ed08e019fc1110464cfa66635c";

        var response = await ExecuteRequest(method, path, data, [
            (HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01"),
        ]);

        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { expectedTraceId }));
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler(string method, string path, string data)
    {
        using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler));
        activity = a;

        var response = await ExecuteRequest(method, path, data, [
            (HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01"),
        ]);

        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { a.TraceId }));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);
        _ = services.AddMvc();

        _ = services.AddConquerorContext();

        _ = services.AddSingleton<TestObservations>();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (ctx, next) =>
        {
            if (activity is not null)
            {
                _ = activity.Activity.Start();

                try
                {
                    await next();
                    return;
                }
                finally
                {
                    activity.Activity.Stop();
                }
            }

            await next();
        });

        _ = app.Use(async (ctx, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        });

        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private ConquerorContext CreateConquerorContext()
    {
        return Resolve<IConquerorContextAccessor>().GetOrCreate();
    }

    private async Task<HttpResponseMessage> ExecuteRequest(string method, string path, string data, IEnumerable<(string Key, string? Value)>? headers = null)
    {
        if (method == HttpMethod.Post.Method)
        {
            using var content = new StringContent(data, null, MediaTypeNames.Application.Json);

            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                {
                    content.Headers.Add(key, value);
                }
            }

            return await HttpClient.PostAsync(path, content);
        }

        using var message = new HttpRequestMessage();
        message.Method = HttpMethod.Get;
        message.RequestUri = new(path, UriKind.Relative);

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                message.Headers.Add(key, value);
            }
        }

        return await HttpClient.SendAsync(message);
    }

    private static DisposableActivity CreateActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        var a = activitySource.CreateActivity(name, ActivityKind.Server)!;
        return new(a, activitySource, activityListener, a);
    }

    private sealed record TestRequest;

    private sealed record TestRequestResponse;

    private sealed record TestRequestWithoutResponse;

    private sealed class TestObservations
    {
        public List<string?> ReceivedTraceIds { get; } = [];

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }

        public Exception? ExceptionToThrow { get; set; }
    }

    [ApiController]
    private sealed class TestHttpCommandController(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations)
        : ControllerBase
    {
        [HttpGet("/api/test")]
        public Task<TestRequestResponse> TestGet(CancellationToken cancellationToken)
        {
            ObserveAndSetContextData(observations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new TestRequestResponse());
        }

        [HttpPost("/api/test")]
        public Task<TestRequestResponse> TestPost(TestRequest command, CancellationToken cancellationToken)
        {
            _ = command;

            ObserveAndSetContextData(observations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new TestRequestResponse());
        }

        [HttpPost("/api/testWithoutPayload")]
        [SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "one endpoint it GET, other is POST")]
        public Task<TestRequestResponse> TestPostWithoutPayload(CancellationToken cancellationToken)
        {
            ObserveAndSetContextData(observations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new TestRequestResponse());
        }

        [HttpPost("/api/testWithoutResponse")]
        public Task TestPostWithoutResponse(TestRequestWithoutResponse command, CancellationToken cancellationToken)
        {
            _ = command;

            ObserveAndSetContextData(observations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        [HttpPost("/api/testWithoutResponseWithoutPayload")]
        public Task TestPostWithoutPayloadWithoutResponse(CancellationToken cancellationToken)
        {
            ObserveAndSetContextData(observations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        private static void ObserveAndSetContextData(TestObservations testObservations, IConquerorContextAccessor conquerorContextAccessor)
        {
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.GetTraceId());
            testObservations.ReceivedDownstreamContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;
            testObservations.ReceivedBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;

            if (testObservations.ShouldAddUpstreamData)
            {
                foreach (var item in ContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.UpstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
                }

                foreach (var item in InProcessContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.UpstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
                }
            }

            if (testObservations.ShouldAddBidirectionalData)
            {
                foreach (var item in ContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
                }

                foreach (var item in InProcessContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
                }
            }

            if (testObservations.ExceptionToThrow is not null)
            {
                throw testObservations.ExceptionToThrow;
            }
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = [typeof(TestHttpCommandController).GetTypeInfo()];
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpCommandController);
    }

    private sealed class DisposableActivity(Activity activity, params IDisposable[] disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

        public Activity Activity { get; } = activity;

        public string TraceId => Activity.TraceId.ToString();

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
