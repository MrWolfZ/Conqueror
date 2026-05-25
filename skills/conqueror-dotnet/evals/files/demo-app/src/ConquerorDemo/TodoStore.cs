namespace ConquerorDemo;

using System.Collections.Concurrent;

public sealed class TodoStore
{
    private readonly ConcurrentDictionary<Guid, Todo> todos = new();

    public Task<Todo> Create(string title, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var todo = new Todo(Guid.NewGuid(), title, IsCompleted: false);
        todos[todo.Id] = todo;
        return Task.FromResult(todo);
    }

    public Task<Todo?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        todos.TryGetValue(id, out var todo);
        return Task.FromResult(todo);
    }

    public Task<IReadOnlyCollection<Todo>> GetAll(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyCollection<Todo>>(todos.Values.ToArray());
    }

    public Task<int> ArchiveCompleted(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var archivedCount = 0;

        foreach (var todo in todos.Values.Where(t => t.IsCompleted))
        {
            if (todos.TryRemove(todo.Id, out _))
            {
                archivedCount += 1;
            }
        }

        return Task.FromResult(archivedCount);
    }

    public Task MarkCompleted(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        todos.AddOrUpdate(
            id,
            static key => new Todo(key, "imported", IsCompleted: true),
            static (_, todo) => todo with { IsCompleted = true });

        return Task.CompletedTask;
    }
}
