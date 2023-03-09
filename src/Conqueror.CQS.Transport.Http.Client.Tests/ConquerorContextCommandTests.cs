using System.Diagnostics;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests;

[TestFixture]
[NonParallelizable]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public class ConquerorContextCommandTests : TestBase
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

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndContextDataInHandler_DataAreReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextData, context.UpstreamContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndContextDataInHandlerWithoutResponse_DataAreReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextData, context.UpstreamContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithData_ContextIsReceivedInHandler()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithData_ContextIsReceivedInHandlerAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var observations = Resolve<TestObservations>();

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithData_ContextIsReceivedInHandlerWithoutResponse()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithData_ContextIsReceivedInHandlerWithoutResponseAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var observations = Resolve<TestObservations>();

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithData_ContextIsReceivedInDifferentHandlersAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var observations = Resolve<TestObservations>();

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();
        var handler2 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler2.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
    }

    [Test]
    public async Task GivenContextDataInHandler_ContextIsReceivedInOuterHandler()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        var receivedContextData = ResolveOnClient<TestObservations>().ReceivedOuterContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenContextDataInHandlerWithoutResponse_ContextIsReceivedInOuterHandler()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new(), CancellationToken.None);

        var receivedContextData = ResolveOnClient<TestObservations>().ReceivedOuterContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenContextDataInOuterHandler_ContextIsReceivedInHandler()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterDownstreamData = true;

        var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenContextDataInOuterHandler_ContextIsReceivedInHandlerWithoutResponse()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterDownstreamData = true;

        var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new(), CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenCommand_SameCommandIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedCommandIds, Resolve<TestObservations>().ReceivedCommandIds);
    }

    [Test]
    public async Task GivenCommandWithoutResponse_SameCommandIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedCommandIds, Resolve<TestObservations>().ReceivedCommandIds);
    }

    [Test]
    public async Task GivenCommandWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenCommandWithoutResponseWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenCommandWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenCommandWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenCommandWithoutResponseWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenCommandWithoutResponseWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenCommandWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenCommandWithoutResponseWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddSingleton<TestObservations>();
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSHttpClientServices(o =>
        {
            _ = o.UseHttpClient(HttpClient);

            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        });

        var baseAddress = new Uri("http://localhost");

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => new WrapperCommandTransportClient(b.UseHttp(baseAddress),
                                                                                                                                         b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                                         b.ServiceProvider.GetRequiredService<TestObservations>()))
                    .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b => new WrapperCommandTransportClient(b.UseHttp(baseAddress),
                                                                                                                                   b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                                   b.ServiceProvider.GetRequiredService<TestObservations>()));

        _ = services.AddConquerorCommandHandler<OuterTestCommandHandler>()
                    .AddConquerorCommandHandler<OuterTestCommandWithoutResponseHandler>()
                    .AddSingleton<TestObservations>();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (_, next) =>
        {
            // prevent leaking of client-side activity to server
            Activity.Current = null;

            await next();
        });

        _ = app.UseRouting();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private static DisposableActivity StartActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = StartActivityListener();

        var activity = activitySource.StartActivity()!;
        return new(activity.TraceId.ToString(), activitySource, activityListener, activity);
    }

    private static IDisposable StartActivityListener(string? activityName = null)
    {
        var activityListener = new ActivityListener
        {
            ShouldListenTo = activity => activityName == null || activity.Name == activityName,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        return activityListener;
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
    public sealed record TestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

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
            testObservations.ReceivedCommandIds.Add(conquerorContextAccessor.ConquerorContext?.GetCommandId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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
            testObservations.ReceivedCommandIds.Add(conquerorContextAccessor.ConquerorContext?.GetCommandId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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

            return Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed record OuterTestCommand;

    public sealed record OuterTestCommandResponse;

    public sealed class OuterTestCommandHandler : ICommandHandler<OuterTestCommand, OuterTestCommandResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly ICommandHandler<TestCommand, TestCommandResponse> nestedHandler;
        private readonly TestObservations testObservations;

        public OuterTestCommandHandler(IConquerorContextAccessor conquerorContextAccessor,
                                       TestObservations testObservations,
                                       ICommandHandler<TestCommand, TestCommandResponse> nestedHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public async Task<OuterTestCommandResponse> ExecuteCommand(OuterTestCommand command, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedCommandIds.Add(conquerorContextAccessor.ConquerorContext?.GetCommandId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            if (testObservations.ShouldAddOuterDownstreamData)
            {
                foreach (var item in ContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
                }

                foreach (var item in InProcessContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
                }
            }

            _ = await nestedHandler.ExecuteCommand(new(), cancellationToken);
            testObservations.ReceivedOuterContextData = conquerorContextAccessor.ConquerorContext?.UpstreamContextData;
            return new();
        }
    }

    public sealed record OuterTestCommandWithoutResponse;

    public sealed class OuterTestCommandWithoutResponseHandler : ICommandHandler<OuterTestCommandWithoutResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly ICommandHandler<TestCommandWithoutResponse> nestedHandler;
        private readonly TestObservations testObservations;

        public OuterTestCommandWithoutResponseHandler(IConquerorContextAccessor conquerorContextAccessor,
                                                      TestObservations testObservations,
                                                      ICommandHandler<TestCommandWithoutResponse> nestedHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public async Task ExecuteCommand(OuterTestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedCommandIds.Add(conquerorContextAccessor.ConquerorContext?.GetCommandId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            if (testObservations.ShouldAddOuterDownstreamData)
            {
                foreach (var item in ContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
                }

                foreach (var item in InProcessContextData)
                {
                    conquerorContextAccessor.ConquerorContext?.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
                }
            }

            await nestedHandler.ExecuteCommand(new(), cancellationToken);
            testObservations.ReceivedOuterContextData = conquerorContextAccessor.ConquerorContext?.UpstreamContextData;
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedCommandIds { get; } = new();

        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddOuterDownstreamData { get; set; }

        public IConquerorContextData? ReceivedContextData { get; set; }

        public IConquerorContextData? ReceivedOuterContextData { get; set; }
    }

    private sealed class WrapperCommandTransportClient : ICommandTransportClient
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;
        private readonly ICommandTransportClient wrapped;

        public WrapperCommandTransportClient(ICommandTransportClient wrapped,
                                             IConquerorContextAccessor conquerorContextAccessor,
                                             TestObservations testObservations)
        {
            this.wrapped = wrapped;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class
        {
            testObservations.ReceivedCommandIds.Add(conquerorContextAccessor.ConquerorContext?.GetCommandId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            return wrapped.ExecuteCommand<TCommand, TResponse>(command, cancellationToken);
        }
    }

    private sealed class DisposableActivity : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables;

        public DisposableActivity(string traceId, params IDisposable[] disposables)
        {
            TraceId = traceId;
            this.disposables = disposables;
        }

        public string TraceId { get; }

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
