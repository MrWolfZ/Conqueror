using System.Diagnostics;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests;

[TestFixture]
[NonParallelizable]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public class ConquerorContextQueryTests : TestBase
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
    public async Task GivenManuallyCreatedContextOnClientAndUpstreamDataInHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextData, context.UpstreamContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndBidirectionalDataInHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextData, context.ContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndUpstreamDataInPostHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextData, context.UpstreamContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndBidirectionalDataInPostHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextData, context.ContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamData_ContextIsReceivedInHandler()
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

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithBidirectionalData_ContextIsReceivedInHandler()
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

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamData_ContextIsReceivedInHandlerAcrossMultipleInvocations()
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

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData, Is.Empty);
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithBidirectionalData_ContextIsReceivedInHandlerAcrossMultipleInvocations()
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

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamData_ContextIsReceivedInPostHandler()
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

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithBidirectionalData_ContextIsReceivedInPostHandler()
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

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        CollectionAssert.IsSubsetOf(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamData_ContextIsReceivedInPostHandlerAcrossMultipleInvocations()
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

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData, Is.Empty);
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithBidirectionalData_ContextIsReceivedInPostHandlerAcrossMultipleInvocations()
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

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamData_ContextIsReceivedInDifferentHandlersAcrossMultipleInvocations()
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

        var handler1 = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData, Is.Empty);
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithBidirectionalData_ContextIsReceivedInDifferentHandlersAcrossMultipleInvocations()
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

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 3));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    public async Task GivenUpstreamDataInHandler_ContextIsReceivedInOuterHandlerAndClient()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = ResolveOnClient<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedOuterUpstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        CollectionAssert.IsSubsetOf(ContextData, context.UpstreamContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
    }

    [Test]
    public async Task GivenBidirectionalDataInHandler_ContextIsReceivedInOuterHandlerAndClient()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = ResolveOnClient<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedOuterBidirectionalContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        CollectionAssert.IsSubsetOf(ContextData, context.ContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenUpstreamDataInPostHandler_ContextIsReceivedInOuterHandlerAndClient()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = ResolveOnClient<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedOuterUpstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        CollectionAssert.IsSubsetOf(ContextData, context.UpstreamContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenBidirectionalDataInPostHandler_ContextIsReceivedInOuterHandlerAndClient()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = ResolveOnClient<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedOuterBidirectionalContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        CollectionAssert.IsSubsetOf(ContextData, context.ContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenDownstreamDataInOuterHandler_ContextIsReceivedInHandlerButNotInClient()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterDownstreamData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = Resolve<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenBidirectionalDataInOuterHandler_ContextIsReceivedInHandlerAndClient()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterBidirectionalData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = Resolve<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedBidirectionalContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        CollectionAssert.IsSubsetOf(ContextData, context.ContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenDownstreamDataInOuterHandler_ContextIsReceivedInPostHandlerButNotInClient()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterDownstreamData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = Resolve<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedDownstreamContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenBidirectionalDataInOuterHandler_ContextIsReceivedInPostHandlerAndClient()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterBidirectionalData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = Resolve<TestObservations>();

        CollectionAssert.IsSubsetOf(ContextData, observations.ReceivedBidirectionalContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        CollectionAssert.IsSubsetOf(ContextData, context.ContextData.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenQuery_SameQueryIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedQueryIds, Resolve<TestObservations>().ReceivedQueryIds);
    }

    [Test]
    public async Task GivenPostQuery_SameQueryIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedQueryIds, Resolve<TestObservations>().ReceivedQueryIds);
    }

    [Test]
    public async Task GivenQueryWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenPostQueryWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenPostQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenPostQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenQueryWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenPostQueryWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler>()
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

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => new WrapperQueryTransportClient(b.UseHttp(baseAddress),
                                                                                                                               b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                               b.ServiceProvider.GetRequiredService<TestObservations>()))
                    .AddConquerorQueryClient<IQueryHandler<TestPostQuery, TestQueryResponse>>(b => new WrapperQueryTransportClient(b.UseHttp(baseAddress),
                                                                                                                                   b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                                   b.ServiceProvider.GetRequiredService<TestObservations>()));

        _ = services.AddConquerorQueryHandler<OuterTestQueryHandler>()
                    .AddConquerorQueryHandler<OuterTestPostQueryHandler>()
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

    [HttpQuery]
    public sealed record TestQuery
    {
        public int Payload { get; init; }
    }

    public sealed record TestQueryResponse
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true)]
    public sealed record TestPostQuery
    {
        public int Payload { get; init; }
    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestQueryHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
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

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
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

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed record OuterTestQuery;

    public sealed record OuterTestQueryResponse;

    public sealed class OuterTestQueryHandler : IQueryHandler<OuterTestQuery, OuterTestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryHandler<TestQuery, TestQueryResponse> nestedHandler;
        private readonly TestObservations testObservations;

        public OuterTestQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                     TestObservations testObservations,
                                     IQueryHandler<TestQuery, TestQueryResponse> nestedHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
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

            if (testObservations.ShouldAddOuterBidirectionalData)
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

            _ = await nestedHandler.ExecuteQuery(new(), cancellationToken);
            testObservations.ReceivedOuterUpstreamContextData = conquerorContextAccessor.ConquerorContext?.UpstreamContextData;
            testObservations.ReceivedOuterBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;
            return new();
        }
    }

    public sealed record OuterTestPostQuery;

    public sealed class OuterTestPostQueryHandler : IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler;
        private readonly TestObservations testObservations;

        public OuterTestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                         TestObservations testObservations,
                                         IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestPostQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
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

            if (testObservations.ShouldAddOuterBidirectionalData)
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

            _ = await nestedHandler.ExecuteQuery(new(), cancellationToken);
            testObservations.ReceivedOuterUpstreamContextData = conquerorContextAccessor.ConquerorContext?.UpstreamContextData;
            testObservations.ReceivedOuterBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;
            return new();
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedQueryIds { get; } = new();

        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public bool ShouldAddOuterDownstreamData { get; set; }

        public bool ShouldAddOuterBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }

        public IConquerorContextData? ReceivedOuterUpstreamContextData { get; set; }

        public IConquerorContextData? ReceivedOuterBidirectionalContextData { get; set; }
    }

    private sealed class WrapperQueryTransportClient : IQueryTransportClient
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;
        private readonly IQueryTransportClient wrapped;

        public WrapperQueryTransportClient(IQueryTransportClient wrapped,
                                           IConquerorContextAccessor conquerorContextAccessor,
                                           TestObservations testObservations)
        {
            this.wrapped = wrapped;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
            where TQuery : class
        {
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            return wrapped.ExecuteQuery<TQuery, TResponse>(query, cancellationToken);
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
