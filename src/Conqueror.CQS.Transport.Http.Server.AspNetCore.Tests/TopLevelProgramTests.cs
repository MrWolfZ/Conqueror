using System.Net;
using System.Net.Http.Json;
using Conqueror.Common;
using Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in TearDown")]
public sealed class TopLevelProgramTests : IDisposable
{
    private readonly WebApplicationFactory<Program> applicationFactory;

    public TopLevelProgramTests()
    {
        applicationFactory = new();

        HttpClient = applicationFactory.CreateClient();
    }

    private HttpClient HttpClient { get; }

    public void Dispose()
    {
        applicationFactory.Dispose();
        HttpClient.Dispose();
    }

    [Test]
    public async Task GivenTopLevelProgramWithQueryHandler_CallingQueryHandlerViaHttpWorks()
    {
        var response = await HttpClient.GetAsync("/api/queries/topLevelTest?payload=10");
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TopLevelTestQueryResponse>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenTopLevelProgramWithQueryHandler_CallingQueryHandlerWithContextDataWorks()
    {
        using var conquerorContext = applicationFactory.Services.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.ContextData.Set("testKey", "testValue", ConquerorContextDataScope.AcrossTransports);

        using var msg = new HttpRequestMessage();
        msg.Method = HttpMethod.Get;
        msg.RequestUri = new("/api/queries/topLevelTest?payload=10", UriKind.Relative);
        msg.Headers.Add(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData());

        var response = await HttpClient.SendAsync(msg);
        await response.AssertStatusCode(HttpStatusCode.OK);
        _ = await response.Content.ReadFromJsonAsync<TopLevelTestQueryResponse>();

        var responseContextHeader = response.Headers.GetValues(HttpConstants.ConquerorContextHeaderName).ToList();
        Assert.That(responseContextHeader, Is.Not.Null);

        conquerorContext.ContextData.Clear();
        conquerorContext.DecodeContextData(responseContextHeader);

        Assert.That(conquerorContext.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(new Dictionary<string, string> { { "testKey", "testValue" } }));
    }

    [Test]
    public async Task GivenTopLevelProgramWithCustomQueryEndpoint_CallingQueryHandlerViaHttpWorks()
    {
        var response = await HttpClient.GetAsync("/customQueryEndpoint?payload=10");
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TopLevelTestQueryResponse>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenTopLevelProgramWithCustomQueryEndpoint_CallingQueryHandlerWithContextDataWorks()
    {
        using var conquerorContext = applicationFactory.Services.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.ContextData.Set("testKey", "testValue", ConquerorContextDataScope.AcrossTransports);

        using var msg = new HttpRequestMessage();
        msg.Method = HttpMethod.Get;
        msg.RequestUri = new("/customQueryEndpoint?payload=10", UriKind.Relative);
        msg.Headers.Add(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData());

        var response = await HttpClient.SendAsync(msg);
        await response.AssertStatusCode(HttpStatusCode.OK);
        _ = await response.Content.ReadFromJsonAsync<TopLevelTestQueryResponse>();

        var responseContextHeader = response.Headers.GetValues(HttpConstants.ConquerorContextHeaderName).ToList();
        Assert.That(responseContextHeader, Is.Not.Null);

        conquerorContext.ContextData.Clear();
        conquerorContext.DecodeContextData(responseContextHeader);

        Assert.That(conquerorContext.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(new Dictionary<string, string> { { "testKey", "testValue" } }));
    }

    [Test]
    public async Task GivenTopLevelProgramWithCommandHandler_CallingCommandHandlerViaHttpWorks()
    {
        var response = await HttpClient.PostAsJsonAsync("/api/commands/topLevelTest", new TopLevelTestCommand(10));
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TopLevelTestCommandResponse>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenTopLevelProgramWithCommandHandler_CallingCommandHandlerWithContextDataWorks()
    {
        using var conquerorContext = applicationFactory.Services.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.ContextData.Set("testKey", "testValue", ConquerorContextDataScope.AcrossTransports);

        using var content = JsonContent.Create(new TopLevelTestCommand(10));

        using var msg = new HttpRequestMessage();
        msg.Method = HttpMethod.Post;
        msg.RequestUri = new("/api/commands/topLevelTest", UriKind.Relative);
        msg.Headers.Add(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData());
        msg.Content = content;

        var response = await HttpClient.SendAsync(msg);
        await response.AssertStatusCode(HttpStatusCode.OK);
        _ = await response.Content.ReadFromJsonAsync<TopLevelTestCommandResponse>();

        var responseContextHeader = response.Headers.GetValues(HttpConstants.ConquerorContextHeaderName).ToList();
        Assert.That(responseContextHeader, Is.Not.Null);

        conquerorContext.ContextData.Clear();
        conquerorContext.DecodeContextData(responseContextHeader);

        Assert.That(conquerorContext.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(new Dictionary<string, string> { { "testKey", "testValue" } }));
    }

    [Test]
    public async Task GivenTopLevelProgramWithCustomCommandEndpoint_CallingCommandHandlerViaHttpWorks()
    {
        var response = await HttpClient.PostAsJsonAsync("/customCommandEndpoint", new TopLevelTestCommand(10));
        await response.AssertStatusCode(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TopLevelTestCommandResponse>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenTopLevelProgramWithCustomCommandEndpoint_CallingCommandHandlerWithContextDataWorks()
    {
        using var conquerorContext = applicationFactory.Services.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.ContextData.Set("testKey", "testValue", ConquerorContextDataScope.AcrossTransports);

        using var content = JsonContent.Create(new TopLevelTestCommand(10));

        using var msg = new HttpRequestMessage();
        msg.Method = HttpMethod.Post;
        msg.RequestUri = new("/customCommandEndpoint", UriKind.Relative);
        msg.Headers.Add(HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData());
        msg.Content = content;

        var response = await HttpClient.SendAsync(msg);
        await response.AssertStatusCode(HttpStatusCode.OK);
        _ = await response.Content.ReadFromJsonAsync<TopLevelTestCommandResponse>();

        var responseContextHeader = response.Headers.GetValues(HttpConstants.ConquerorContextHeaderName).ToList();
        Assert.That(responseContextHeader, Is.Not.Null);

        conquerorContext.ContextData.Clear();
        conquerorContext.DecodeContextData(responseContextHeader);

        Assert.That(conquerorContext.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(new Dictionary<string, string> { { "testKey", "testValue" } }));
    }
}
