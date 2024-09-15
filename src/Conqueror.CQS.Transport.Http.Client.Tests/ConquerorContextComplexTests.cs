using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests;

[TestFixture]
[NonParallelizable]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public class ConquerorContextComplexTests : TestBase
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

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamData_ContextIsReceivedInDifferentCommandAndQueryHandlersAcrossMultipleInvocations()
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

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(observations.ReceivedBidirectionalContextData, Is.Empty);
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithBidirectionalData_ContextIsReceivedInDifferentCommandAndQueryHandlersAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var observations = Resolve<TestObservations>();

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, object>(t.Key, t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamAndBidirectionalData_ContextIsReceivedInDifferentCommandAndQueryHandlersAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        foreach (var item in ContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
            context.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
        }

        foreach (var item in InProcessContextData)
        {
            context.DownstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
            context.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
        }

        var observations = Resolve<TestObservations>();

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());
        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());
        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());
        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 6));
        Assert.That(ContextData, Is.SubsetOf(Resolve<TestObservations>().ReceivedDownstreamContextData!.Select(t => new KeyValuePair<string, object>(t.Key, t.Value))));
        Assert.That(ContextData, Is.SubsetOf(Resolve<TestObservations>().ReceivedBidirectionalContextData!.Select(t => new KeyValuePair<string, object>(t.Key, t.Value))));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientAndHandlerWhichSetsData_ContextDataIsReceivedInFollowingHandler()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var observations = Resolve<TestObservations>();

        observations.ShouldAddBidirectionalData = true;

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, object>(t.Key, t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClient_SameTraceIdIsReceivedInDifferentCommandAndQueryHandlersAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var observations = Resolve<TestObservations>();

        var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(observations.ReceivedTraceIds, Is.EquivalentTo(new[] { context.TraceId, context.TraceId, context.TraceId }));
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorCommandHandler<TestCommandHandler>()
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

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.UseHttp(baseAddress))
                    .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.UseHttp(baseAddress));
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

    public sealed class TestQueryHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations)
        : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            observations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            observations.ReceivedDownstreamContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;
            observations.ReceivedBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;

            if (observations.ShouldAddUpstreamData)
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

            if (observations.ShouldAddBidirectionalData)
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

            return Task.FromResult(new TestQueryResponse());
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

    public sealed class TestCommandHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations)
        : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            observations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            observations.ReceivedDownstreamContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;
            observations.ReceivedBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;

            if (observations.ShouldAddUpstreamData)
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

            if (observations.ShouldAddBidirectionalData)
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

            return Task.FromResult(new TestCommandResponse());
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedTraceIds { get; } = [];

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }
    }
}
