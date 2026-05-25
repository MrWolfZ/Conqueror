namespace ConquerorDemo.Tests;

using Xunit;

public sealed class TodoStoreTests
{
    [Fact]
    public async Task CreatedTodoCanBeReadBack()
    {
        var store = new TodoStore();

        var created = await store.Create("write skill eval", CancellationToken.None);

        var loaded = await store.Get(created.Id, CancellationToken.None);

        Assert.Equal(created, loaded);
    }

    [Fact]
    public async Task ArchiveCompletedRemovesCompletedTodos()
    {
        var store = new TodoStore();
        var todo = await store.Create("archive me", CancellationToken.None);
        await store.MarkCompleted(todo.Id, CancellationToken.None);

        var archivedCount = await store.ArchiveCompleted(CancellationToken.None);
        var loaded = await store.Get(todo.Id, CancellationToken.None);

        Assert.Equal(1, archivedCount);
        Assert.Null(loaded);
    }
}
