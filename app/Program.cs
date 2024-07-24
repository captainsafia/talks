using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var todos = app.MapGroup("/todos");
List<Todo> tasks = [new Todo(1, "Buy milk", false), new Todo(2, "Buy bread", true)];

todos.MapGet("/", () => tasks);
todos.MapGet("/{id}", (int id) => tasks.FirstOrDefault(x => x.Id == id));
todos.MapPost("/", (Todo todo) => 
{
    tasks.Add(todo);
    return TypedResults.Created($"/todos/{todo.Id}", todo);
});

app.Run();

record Todo(int Id, string Title, bool Completed);
