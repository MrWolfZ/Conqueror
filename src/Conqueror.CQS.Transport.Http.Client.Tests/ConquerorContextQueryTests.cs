using System.Diagnostics;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests;

[TestFixture]
[NonParallelizable]
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
    public async Task GivenManuallyCreatedContextOnClientAndUpstreamDataInHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(context.UpstreamContextData.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndBidirectionalDataInHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(context.UpstreamContextData, Is.Empty);
        Assert.That(context.ContextData.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndUpstreamAndBidirectionalDataInHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(context.UpstreamContextData.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(context.ContextData.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndUpstreamDataInPostHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(context.UpstreamContextData.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(context.ContextData, Is.Empty);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndBidirectionalDataInPostHandler_DataIsReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(context.ContextData.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
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

        Assert.That(receivedContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedContextData!.AsKeyValuePairs<string>()));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
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

        Assert.That(receivedContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedContextData!.AsKeyValuePairs<string>()));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.AsKeyValuePairs<string>(), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamAndBidirectionalData_ContextIsReceivedInHandler()
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

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedDownstreamContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedDownstreamContextData, Is.Not.Null);
        Assert.That(receivedBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedDownstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(receivedBidirectionalContextData!.AsKeyValuePairs<string>()));
    }

    [Test]
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
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [Test]
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
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.AsKeyValuePairs<string>(), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithDownstreamAndBidirectionalData_ContextIsReceivedInHandlerAcrossMultipleInvocations()
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

        observations.ShouldAddUpstreamData = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());
        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());
        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedDownstreamContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());
        allReceivedKeys.AddRange(observations.ReceivedBidirectionalContextData?.Select(t => t.Key).Where(ContextData.ContainsKey) ?? Array.Empty<string>());

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextData.Count * 6));
        Assert.That(ContextData, Is.SubsetOf(Resolve<TestObservations>().ReceivedDownstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(Resolve<TestObservations>().ReceivedBidirectionalContextData!.AsKeyValuePairs<string>()));
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

        Assert.That(receivedContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedContextData!.AsKeyValuePairs<string>()));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
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

        Assert.That(receivedContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedContextData!.AsKeyValuePairs<string>()));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.AsKeyValuePairs<string>(), Is.Not.SubsetOf(ContextData));
    }

    [Test]
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
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [Test]
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
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.AsKeyValuePairs<string>(), Is.Not.SubsetOf(ContextData));
    }

    [Test]
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
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports(), Is.Empty);
    }

    [Test]
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
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.AsKeyValuePairs<string>(), Is.Not.SubsetOf(ContextData));
    }

    [Test]
    public async Task GivenUpstreamDataInHandler_ContextIsReceivedInOuterHandlerAndClient()
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = ResolveOnClient<TestObservations>();

        Assert.That(observations.ReceivedOuterUpstreamContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedOuterUpstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(context.UpstreamContextData.AsKeyValuePairs<string>()));
    }

    [Test]
    public async Task GivenBidirectionalDataInHandler_ContextIsReceivedInOuterHandlerAndClient()
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var observations = ResolveOnClient<TestObservations>();

        Assert.That(observations.ReceivedOuterBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedOuterBidirectionalContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(context.ContextData.AsKeyValuePairs<string>()));
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

        Assert.That(observations.ReceivedOuterUpstreamContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedOuterUpstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(context.UpstreamContextData.AsKeyValuePairs<string>()));
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

        Assert.That(observations.ReceivedOuterBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedOuterBidirectionalContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(context.ContextData.AsKeyValuePairs<string>()));
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

        Assert.That(observations.ReceivedDownstreamContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedDownstreamContextData!.AsKeyValuePairs<string>()));
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

        Assert.That(observations.ReceivedBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedBidirectionalContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(context.ContextData.AsKeyValuePairs<string>()));
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

        Assert.That(observations.ReceivedDownstreamContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedDownstreamContextData!.AsKeyValuePairs<string>()));
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

        Assert.That(observations.ReceivedBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(observations.ReceivedBidirectionalContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(context.ContextData.AsKeyValuePairs<string>()));
        Assert.That(context.UpstreamContextData, Is.Empty);
    }

    [Test]
    public async Task GivenQuery_SameQueryIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedQueryIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedQueryIds));
    }

    [Test]
    public async Task GivenPostQuery_SameQueryIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedQueryIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedQueryIds));
    }

    [Test]
    public async Task GivenQueryWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedTraceIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedTraceIds));
    }

    [Test]
    public async Task GivenPostQueryWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedTraceIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedTraceIds));
    }

    [Test]
    public async Task GivenQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedTraceIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedTraceIds));
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenPostQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenPostQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedTraceIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedTraceIds));
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenQueryWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedTraceIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedTraceIds));
    }

    [Test]
    public async Task GivenPostQueryWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        Assert.That(ResolveOnClient<TestObservations>().ReceivedTraceIds, Is.EquivalentTo(Resolve<TestObservations>().ReceivedTraceIds));
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
        _ = app.UseConqueror();
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

    public sealed class TestQueryHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations)
        : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            observations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
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

    public sealed class TestPostQueryHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations)
        : IQueryHandler<TestPostQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
        {
            observations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
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

    public sealed record OuterTestQuery;

    public sealed record OuterTestQueryResponse;

    public sealed class OuterTestQueryHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        IQueryHandler<TestQuery, TestQueryResponse> nestedHandler)
        : IQueryHandler<OuterTestQuery, OuterTestQueryResponse>
    {
        public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestQuery query, CancellationToken cancellationToken = default)
        {
            observations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            observations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            if (observations.ShouldAddOuterDownstreamData)
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

            if (observations.ShouldAddOuterBidirectionalData)
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
            observations.ReceivedOuterUpstreamContextData = conquerorContextAccessor.ConquerorContext?.UpstreamContextData;
            observations.ReceivedOuterBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;
            return new();
        }
    }

    public sealed record OuterTestPostQuery;

    public sealed class OuterTestPostQueryHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler)
        : IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>
    {
        public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestPostQuery query, CancellationToken cancellationToken = default)
        {
            observations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            observations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            if (observations.ShouldAddOuterDownstreamData)
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

            if (observations.ShouldAddOuterBidirectionalData)
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
            observations.ReceivedOuterUpstreamContextData = conquerorContextAccessor.ConquerorContext?.UpstreamContextData;
            observations.ReceivedOuterBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;
            return new();
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedQueryIds { get; } = [];

        public List<string?> ReceivedTraceIds { get; } = [];

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public bool ShouldAddOuterDownstreamData { get; set; }

        public bool ShouldAddOuterBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }

        public IConquerorContextData? ReceivedOuterUpstreamContextData { get; set; }

        public IConquerorContextData? ReceivedOuterBidirectionalContextData { get; set; }
    }

    private sealed class WrapperQueryTransportClient(
        IQueryTransportClient wrapped,
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations)
        : IQueryTransportClient
    {
        public QueryTransportType TransportType { get; } = new("test", QueryTransportRole.Client);

        public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TQuery : class
        {
            observations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            observations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            return wrapped.ExecuteQuery<TQuery, TResponse>(query, serviceProvider, cancellationToken);
        }
    }

    private sealed class DisposableActivity(string traceId, params IDisposable[] disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

        public string TraceId { get; } = traceId;

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
