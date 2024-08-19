using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<Writer>();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.UseEndpoints();

app.Map("/", (string name, int age) => 
{
    return new OkResult($"You're name is {name} and you're {age} years old.");
})
.Add(builder =>
{
    builder.Metadata.Add(new RequiresAuthorizationMetadata());
});
app.MapPost("/post", (int id, Todo todo) => 
{
    return new OkResult($"Processing on id: {id} with {todo.Title}...");
});
app.Map("/writer", (string location, Writer writer) => 
{
    writer.Write($"You're location is {location}");
    return new OkResult("Check the console!");
});
app.Run();

public class Writer
{
    public void Write(string message)
    {
        Console.WriteLine(message);
    }
}

public record Todo(int Id, string Title, bool IsCompleted);

public class RequiresAuthorizationMetadata { }