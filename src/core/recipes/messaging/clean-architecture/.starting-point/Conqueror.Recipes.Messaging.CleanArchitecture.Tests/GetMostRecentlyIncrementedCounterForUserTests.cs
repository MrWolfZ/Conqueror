namespace Conqueror.Recipes.Messaging.CleanArchitecture.Tests;

[TestFixture]
public class GetMostRecentlyIncrementedCounterForUserTests
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "testUser";

    [Test]
    public async Task GivenExistingUserHistory_WhenGettingMostRecentlyIncrementedCounterForUser_ThenCounterNameIsReturned()
    {
        await using var host = TestHost.Create();

        await host.ResolveOnServer<UserHistoryRepository>().SetMostRecentlyIncrementedCounter(TestUserId, TestCounterName);

        var response = await host.MessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));

        Assert.That(response.CounterName, Is.EqualTo(TestCounterName));
    }

    [Test]
    public async Task GivenNonExistingUserHistory_WhenGettingMostRecentlyIncrementedCounterForUser_ThenNullIsReturned()
    {
        await using var host = TestHost.Create();

        var response = await host.MessageSenders.For(GetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId));

        Assert.That(response.CounterName, Is.Null);
    }
}
