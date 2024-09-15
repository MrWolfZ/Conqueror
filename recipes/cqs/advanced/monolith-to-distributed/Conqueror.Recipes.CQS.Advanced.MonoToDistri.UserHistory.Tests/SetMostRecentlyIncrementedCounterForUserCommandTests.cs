using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Tests;

[TestFixture]
public sealed class SetMostRecentlyIncrementedCounterForUserCommandTests : TestBase
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "testUser";

    private ISetMostRecentlyIncrementedCounterForUserCommandHandler CommandClient => ResolveOnServer<ISetMostRecentlyIncrementedCounterForUserCommandHandler>();

    private IUserHistoryWriteRepository WriteRepository => ResolveOnServer<IUserHistoryWriteRepository>();

    private IUserHistoryReadRepository ReadRepository => ResolveOnServer<IUserHistoryReadRepository>();

    [Test]
    public async Task GivenNonExistingUserHistory_WhenSettingMostRecentlyIncrementedCounterForUser_SetsHistory()
    {
        await CommandClient.Handle(new(TestUserId, TestCounterName));

        var storedCounterName = await ReadRepository.GetMostRecentlyIncrementedCounterByUserId(TestUserId);

        Assert.That(storedCounterName, Is.EqualTo(TestCounterName));
    }

    [Test]
    public async Task GivenExistingUserHistory_WhenSettingMostRecentlyIncrementedCounterForUser_SetsHistory()
    {
        await WriteRepository.SetMostRecentlyIncrementedCounter(TestUserId, "existingCounter");

        await CommandClient.Handle(new(TestUserId, TestCounterName));

        var storedCounterName = await ReadRepository.GetMostRecentlyIncrementedCounterByUserId(TestUserId);

        Assert.That(storedCounterName, Is.EqualTo(TestCounterName));
    }
}
