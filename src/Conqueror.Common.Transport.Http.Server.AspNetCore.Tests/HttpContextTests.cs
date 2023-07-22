using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Net.Http.Headers;

namespace Conqueror.Common.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[NonParallelizable]
public sealed class HttpContextTests : TestBase
{
    private static readonly Dictionary<string, string> ContextData = new()
    {
        { "key1", "value1" },
        { "key2", "value2" },
        { "keyWith,Comma", "value" },
        { "key4", "valueWith,Comma" },
        { "keyWith=Equals", "value" },
        { "key6", "valueWith=Equals" },
    };

    private static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key7", "value1" },
        { "key8", "value2" },
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

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorUpstreamContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        var receivedData = ConquerorContextDataFormatter.Parse(values!);

        Assert.That(receivedData.AsKeyValuePairs(), Is.EquivalentTo(ContextData));
        Assert.That(response.Headers.Contains(HttpConstants.ConquerorContextHeaderName), Is.False);
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

        var receivedData = ConquerorContextDataFormatter.Parse(values!);

        Assert.That(receivedData.AsKeyValuePairs(), Is.EquivalentTo(ContextData));
        Assert.That(response.Headers.Contains(HttpConstants.ConquerorUpstreamContextHeaderName), Is.False);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorDownstreamContextRequestHeader_DataIsReceivedByHandler(string method, string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var response = await ExecuteRequest(method, path, data, new Dictionary<string, string?>
        {
            { HttpConstants.ConquerorDownstreamContextHeaderName, ConquerorContextDataFormatter.Format(conquerorContext.DownstreamContextData) },
        });

        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;

        Assert.That(receivedContextData?.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData, Is.Empty);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorBidirectionalContextRequestHeader_DataIsReceivedByHandler(string method, string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var response = await ExecuteRequest(method, path, data, new Dictionary<string, string?>
        {
            { HttpConstants.ConquerorContextHeaderName, ConquerorContextDataFormatter.Format(conquerorContext.ContextData) },
        });

        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedContextData?.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData, Is.Empty);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenInvalidConquerorDownstreamContextRequestHeader_ReturnsBadRequest(string method, string path, string data)
    {
        var response = await ExecuteRequest(method, path, data, new Dictionary<string, string?>
        {
            { HttpConstants.ConquerorDownstreamContextHeaderName, "foo=bar" },
        });

        await response.AssertStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenInvalidConquerorBidirectionalContextRequestHeader_ReturnsBadRequest(string method, string path, string data)
    {
        var response = await ExecuteRequest(method, path, data, new Dictionary<string, string?>
        {
            { HttpConstants.ConquerorContextHeaderName, "foo=bar" },
        });

        await response.AssertStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("GET", "/api/test", "")]
    [TestCase("POST", "/api/test", "{}")]
    [TestCase("POST", "/api/testWithoutResponse", "{}")]
    [TestCase("POST", "/api/testWithoutPayload", "")]
    [TestCase("POST", "/api/testWithoutResponseWithoutPayload", "")]
    public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandler(string method, string path, string data)
    {
        const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";

        var response = await ExecuteRequest(method, path, data, new Dictionary<string, string?>
        {
            { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
        });

        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { testTraceId }));
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

        var response = await ExecuteRequest(method, path, data, new Dictionary<string, string?>
        {
            { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
        });

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

        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private async Task<HttpResponseMessage> ExecuteRequest(string method, string path, string data, IReadOnlyDictionary<string, string?>? headers = null)
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

        using var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
        };

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                message.Headers.Add(key, value);
            }
        }

        return await HttpClient.SendAsync(message);
    }

    private static void ObserveAndSetContextData(TestObservations testObservations, IConquerorContextAccessor conquerorContextAccessor)
    {
        // TODO: find a better solution for this
        _ = conquerorContextAccessor.ConquerorContext?.DownstreamContextData.Remove("Conqueror.Common.ConquerorContextCommonExtensions.SignalExecutionFromTransport");

        testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
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
        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }
    }

    [ApiController]
    private sealed class TestHttpCommandController : ControllerBase
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestHttpCommandController(IConquerorContextAccessor conquerorContextAccessor, TestObservations observations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            testObservations = observations;
        }

        [HttpGet("/api/test")]
        public Task<TestRequestResponse> TestGet(CancellationToken cancellationToken)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new TestRequestResponse());
        }

        [HttpPost("/api/test")]
        public Task<TestRequestResponse> TestPost(TestRequest command, CancellationToken cancellationToken)
        {
            _ = command;

            ObserveAndSetContextData(testObservations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new TestRequestResponse());
        }

        [HttpPost("/api/testWithoutPayload")]
        [SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "one endpoint it GET, other is POST")]
        public Task<TestRequestResponse> TestPostWithoutPayload(CancellationToken cancellationToken)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new TestRequestResponse());
        }

        [HttpPost("/api/testWithoutResponse")]
        public Task TestPostWithoutResponse(TestRequestWithoutResponse command, CancellationToken cancellationToken)
        {
            _ = command;

            ObserveAndSetContextData(testObservations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        [HttpPost("/api/testWithoutResponseWithoutPayload")]
        public Task TestPostWithoutPayloadWithoutResponse(CancellationToken cancellationToken)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestHttpCommandController).GetTypeInfo() };
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpCommandController);
    }

    private sealed class DisposableActivity : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables;

        public DisposableActivity(Activity activity, params IDisposable[] disposables)
        {
            Activity = activity;
            this.disposables = disposables;
        }

        public Activity Activity { get; }

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
