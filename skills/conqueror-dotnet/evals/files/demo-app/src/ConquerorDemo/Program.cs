using ConquerorDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TodoStore>();

var app = builder.Build();

app.MapPost("/todos", async (CreateTodoRequest request, TodoStore store, CancellationToken cancellationToken) =>
{
    var todo = await store.Create(request.Title, cancellationToken);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapGet("/todos/{id:guid}", async (Guid id, TodoStore store, CancellationToken cancellationToken) =>
{
    var todo = await store.Get(id, cancellationToken);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

app.MapGet("/todos", async (TodoStore store, CancellationToken cancellationToken) =>
{
    var todos = await store.GetAll(cancellationToken);
    return Results.Ok(todos);
});

await app.RunAsync();
