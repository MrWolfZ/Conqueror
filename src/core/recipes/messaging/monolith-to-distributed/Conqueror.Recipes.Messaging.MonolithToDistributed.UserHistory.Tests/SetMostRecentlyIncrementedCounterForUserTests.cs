namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests;

[TestFixture]
public class SetMostRecentlyIncrementedCounterForUserTests
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "testUser";

    [Test]
    public async Task WhenSettingMostRecentlyIncrementedCounterForUser_ThenUserHistoryIsUpdated()
    {
        await using var host = TestHost.Create();

        await host.MessageSenders.For(SetMostRecentlyIncrementedCounterForUser.T).Handle(new(TestUserId, TestCounterName));

        var storedCounterName = await host.ResolveOnServer<IUserHistoryReadRepository>().GetMostRecentlyIncrementedCounterByUserId(TestUserId);

        Assert.That(storedCounterName, Is.EqualTo(TestCounterName));
    }
}
