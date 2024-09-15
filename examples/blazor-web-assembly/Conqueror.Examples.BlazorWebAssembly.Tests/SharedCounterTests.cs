using System.ComponentModel.DataAnnotations;
using System.Net;
using Conqueror.Examples.BlazorWebAssembly.Domain;

namespace Conqueror.Examples.BlazorWebAssembly.Tests;

public sealed class SharedCounterTests : TestBase
{
    private IIncrementSharedCounterValueCommandHandler IncrementHandler => CreateCommandHttpClient<IIncrementSharedCounterValueCommandHandler>();

    private IGetSharedCounterValueQueryHandler GetValueHandler => CreateQueryHttpClient<IGetSharedCounterValueQueryHandler>();

    [Test]
    public async Task GivenCounterValue_WhenGettingValue_ReturnsValue()
    {
        var sharedCounter = Resolve<SharedCounter>();
        var expectedValue = sharedCounter.IncrementBy(10);

        var response = await GetValueHandler.Handle(new(), CancellationToken.None);

        Assert.That(expectedValue, Is.EqualTo(response.Value));
    }

    [Test]
    public async Task GivenCounterValue_WhenIncrementingCounter_IncrementsCounterBySpecifiedValue()
    {
        var sharedCounter = Resolve<SharedCounter>();
        var existingValue = sharedCounter.IncrementBy(10);
        var incrementBy = 5;

        var response = await IncrementHandler.Handle(new() { IncrementBy = incrementBy }, CancellationToken.None);

        Assert.That(existingValue + incrementBy, Is.EqualTo(response.ValueAfterIncrement));
        Assert.That(sharedCounter.GetValue(), Is.EqualTo(response.ValueAfterIncrement));
    }

    [Test]
    public async Task GivenCounterValue_WhenIncrementingCounter_EventIsPublishedToEventHub()
    {
        var sharedCounter = Resolve<SharedCounter>();
        var eventHub = Resolve<TestEventHub>();

        var existingValue = sharedCounter.IncrementBy(10);
        var incrementBy = 5;

        await IncrementHandler.Handle(new() { IncrementBy = incrementBy }, CancellationToken.None);

        Assert.That(eventHub.ObservedEvents, Is.EquivalentTo(new[] { new SharedCounterIncrementedEvent(existingValue + incrementBy, incrementBy) }));
    }

    [Test]
    public async Task GivenCounterValue_WhenIncrementingCounter_EventIsStoredInInMemoryEventStore()
    {
        var sharedCounter = Resolve<SharedCounter>();
        var eventStore = Resolve<InMemoryEventStore>();

        var existingValue = sharedCounter.IncrementBy(10);
        var incrementBy = 5;

        await IncrementHandler.Handle(new() { IncrementBy = incrementBy }, CancellationToken.None);

        Assert.That(eventStore.GetEvents(), Is.EquivalentTo(new[] { new SharedCounterIncrementedEvent(existingValue + incrementBy, incrementBy) }));
    }

    [Test]
    public void GivenInvalidIncrementByValue_WhenIncrementingCounter_IncrementFailsWithBadRequest()
    {
        var exception = Assert.ThrowsAsync<HttpCommandFailedException>(() => IncrementHandler.Handle(new() { IncrementBy = -1 }, CancellationToken.None));

        Assert.That(exception, Is.Not.Null);
        Assert.That(HttpStatusCode.BadRequest, Is.EqualTo(exception?.Response?.StatusCode));
    }

    [Test]
    public void GivenInvalidIncrementByValue_WhenIncrementingCounterDirectly_IncrementFailsWithValidationException()
    {
        Assert.ThrowsAsync<ValidationException>(() => Resolve<IIncrementSharedCounterValueCommandHandler>().Handle(new() { IncrementBy = -1 }, CancellationToken.None));
    }
}
