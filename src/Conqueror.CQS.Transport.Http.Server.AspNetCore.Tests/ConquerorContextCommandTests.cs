using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using Conqueror.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Net.Http.Headers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[NonParallelizable]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class ConquerorContextCommandTests : TestBase
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

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenUpstreamContextData_DataIsReturnedInHeader(string path, string data)
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var content = new StringContent(data, null, MediaTypeNames.Application.Json);
        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorUpstreamContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        var receivedData = ConquerorContextDataFormatter.Parse(values!);

        Assert.That(receivedData.AsKeyValuePairs(), Is.EquivalentTo(ContextData));

        Assert.That(response.Headers.Contains(HttpConstants.ConquerorContextHeaderName), Is.False);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenBidirectionalContextData_DataIsReturnedInHeader(string path, string data)
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var content = new StringContent(data, null, MediaTypeNames.Application.Json);
        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        var receivedData = ConquerorContextDataFormatter.Parse(values!);

        Assert.That(receivedData.AsKeyValuePairs(), Is.EquivalentTo(ContextData));

        Assert.That(response.Headers.Contains(HttpConstants.ConquerorUpstreamContextHeaderName), Is.False);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorDownstreamContextRequestHeader_DataIsReceivedByHandler(string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers = { { HttpConstants.ConquerorDownstreamContextHeaderName, ConquerorContextDataFormatter.Format(conquerorContext.DownstreamContextData) } },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;

        Assert.That(receivedContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedContextData!.AsKeyValuePairs<string>()));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenConquerorBidirectionalContextRequestHeader_DataIsReceivedByHandler(string path, string data)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers = { { HttpConstants.ConquerorContextHeaderName, ConquerorContextDataFormatter.Format(conquerorContext.ContextData) } },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedContextData?.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenInvalidConquerorDownstreamContextRequestHeader_ReturnsBadRequest(string path, string data)
    {
        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers = { { HttpConstants.ConquerorDownstreamContextHeaderName, "foo=bar" } },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenInvalidConquerorBidirectionalContextRequestHeader_ReturnsBadRequest(string path, string data)
    {
        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers = { { HttpConstants.ConquerorContextHeaderName, "foo=bar" } },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenCommandIdInContext_CommandIdIsObservedByHandler(string path, string data)
    {
        const string commandId = "test-command";
        
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetCommandId(commandId);

        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers = { { HttpConstants.ConquerorDownstreamContextHeaderName, ConquerorContextDataFormatter.Format(conquerorContext.DownstreamContextData) } },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var receivedCommandIds = Resolve<TestObservations>().ReceivedCommandIds;

        Assert.That(receivedCommandIds, Is.EqualTo(new[] { commandId }));
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenNoCommandIdInContext_NonEmptyCommandIdIsObservedByHandler(string path, string data)
    {
        using var content = new StringContent(data, null, MediaTypeNames.Application.Json);

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var receivedCommandIds = Resolve<TestObservations>().ReceivedCommandIds;

        Assert.That(receivedCommandIds, Has.Count.EqualTo(1));
        Assert.That(receivedCommandIds[0], Is.Not.Null.And.Not.Empty);
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandler(string path, string data)
    {
        const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";
        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { testTraceId }));
    }

    [Test]
    public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandlerAndNestedHandler()
    {
        const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";
        using var content = new StringContent("{}", null, MediaTypeNames.Application.Json)
        {
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
        };

        var response = await HttpClient.PostAsync("/api/commands/testCommandWithNested", content);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
        Assert.That(receivedTraceIds[0], Is.EqualTo(testTraceId));
        Assert.That(receivedTraceIds[1], Is.EqualTo(testTraceId));
    }

    [TestCase("/api/commands/test", "{}")]
    [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
    [TestCase("/api/commands/testDelegate", "{}")]
    [TestCase("/api/custom/commands/test", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutResponse", "{}")]
    [TestCase("/api/custom/commands/testCommandWithoutPayload", "")]
    [TestCase("/api/custom/commands/testCommandWithoutResponseWithoutPayload", "")]
    public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler(string path, string data)
    {
        using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler));
        activity = a;

        using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
        {
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
        };

        var response = await HttpClient.PostAsync(path, content);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { a.TraceId }));
    }

    [Test]
    public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler()
    {
        using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler));
        activity = a;

        using var content = new StringContent("{}", null, MediaTypeNames.Application.Json)
        {
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
        };

        var response = await HttpClient.PostAsync("/api/commands/testCommandWithNested", content);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
        Assert.That(receivedTraceIds[0], Is.EqualTo(a.TraceId));
        Assert.That(receivedTraceIds[1], Is.EqualTo(a.TraceId));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);

        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutPayload>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutPayload>()
                    .AddConquerorCommandHandler<TestCommandWithNestedCommandHandler>()
                    .AddConquerorCommandHandler<NestedTestCommandHandler>()
                    .AddConquerorCommandHandlerDelegate<TestDelegateCommand, TestDelegateCommandResponse>(async (_, p, _) =>
                    {
                        await Task.CompletedTask;

                        var testObservations = p.GetRequiredService<TestObservations>();
                        var conquerorContextAccessor = p.GetRequiredService<IConquerorContextAccessor>();

                        ObserveAndSetContextData(testObservations, conquerorContextAccessor);

                        return new();
                    })
                    .AddSingleton<TestObservations>();
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

    private static void ObserveAndSetContextData(TestObservations testObservations, IConquerorContextAccessor conquerorContextAccessor)
    {
        testObservations.ReceivedCommandIds.Add(conquerorContextAccessor.ConquerorContext?.GetCommandId());
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

    [HttpCommand]
    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    [HttpCommand]
    public sealed record TestCommandWithoutPayload;

    [HttpCommand]
    public sealed record TestCommandWithoutResponse;

    [HttpCommand]
    public sealed record TestCommandWithoutResponseWithoutPayload;

    [HttpCommand]
    public sealed record TestCommandWithNestedCommand;

    [HttpCommand]
    public sealed record TestDelegateCommand;

    public sealed record TestDelegateCommandResponse;

    public sealed record NestedTestCommand;

    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestCommandHandler(IConquerorContextAccessor conquerorContextAccessor,
                                  TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestCommandHandlerWithoutResponse(IConquerorContextAccessor conquerorContextAccessor,
                                                 TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.CompletedTask;
        }
    }

    public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestCommandHandlerWithoutPayload(IConquerorContextAccessor conquerorContextAccessor,
                                                TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestCommandHandlerWithoutResponseWithoutPayload(IConquerorContextAccessor conquerorContextAccessor,
                                                               TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.CompletedTask;
        }
    }

    public sealed class TestCommandWithNestedCommandHandler : ICommandHandler<TestCommandWithNestedCommand, TestCommandResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly ICommandHandler<NestedTestCommand, TestCommandResponse> nestedHandler;
        private readonly TestObservations testObservations;

        public TestCommandWithNestedCommandHandler(IConquerorContextAccessor conquerorContextAccessor,
                                                   ICommandHandler<NestedTestCommand, TestCommandResponse> nestedHandler,
                                                   TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public Task<TestCommandResponse> ExecuteCommand(TestCommandWithNestedCommand command, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return nestedHandler.ExecuteCommand(new(), cancellationToken);
        }
    }

    public sealed class NestedTestCommandHandler : ICommandHandler<NestedTestCommand, TestCommandResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public NestedTestCommandHandler(IConquerorContextAccessor conquerorContextAccessor,
                                        TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestCommandResponse> ExecuteCommand(NestedTestCommand command, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedCommandIds { get; } = new();

        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }
    }

    [ApiController]
    private sealed class TestHttpCommandController : ControllerBase
    {
        private readonly ICommandHandler<TestCommand, TestCommandResponse> commandHandler;
        private readonly ICommandHandler<TestCommandWithoutPayload, TestCommandResponse> commandWithoutPayloadHandler;
        private readonly ICommandHandler<TestCommandWithoutResponse> commandWithoutResponseHandler;
        private readonly ICommandHandler<TestCommandWithoutResponseWithoutPayload> commandWithoutResponseWithoutPayloadHandler;

        public TestHttpCommandController(ICommandHandler<TestCommand, TestCommandResponse> commandHandler,
                                         ICommandHandler<TestCommandWithoutPayload, TestCommandResponse> commandWithoutPayloadHandler,
                                         ICommandHandler<TestCommandWithoutResponse> commandWithoutResponseHandler,
                                         ICommandHandler<TestCommandWithoutResponseWithoutPayload> commandWithoutResponseWithoutPayloadHandler)
        {
            this.commandHandler = commandHandler;
            this.commandWithoutPayloadHandler = commandWithoutPayloadHandler;
            this.commandWithoutResponseHandler = commandWithoutResponseHandler;
            this.commandWithoutResponseWithoutPayloadHandler = commandWithoutResponseWithoutPayloadHandler;
        }

        [HttpPost("/api/custom/commands/test")]
        public Task<TestCommandResponse> ExecuteTestCommand(TestCommand command, CancellationToken cancellationToken)
        {
            return commandHandler.ExecuteCommand(command, cancellationToken);
        }

        [HttpPost("/api/custom/commands/testCommandWithoutPayload")]
        public Task<TestCommandResponse> ExecuteTestCommandWithoutPayload(CancellationToken cancellationToken)
        {
            return commandWithoutPayloadHandler.ExecuteCommand(new(), cancellationToken);
        }

        [HttpPost("/api/custom/commands/testCommandWithoutResponse")]
        public Task ExecuteTestCommandWithoutResponse(TestCommandWithoutResponse command, CancellationToken cancellationToken)
        {
            return commandWithoutResponseHandler.ExecuteCommand(command, cancellationToken);
        }

        [HttpPost("/api/custom/commands/testCommandWithoutResponseWithoutPayload")]
        public Task ExecuteTestCommandWithoutPayloadWithoutResponse(CancellationToken cancellationToken)
        {
            return commandWithoutResponseWithoutPayloadHandler.ExecuteCommand(new(), cancellationToken);
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
