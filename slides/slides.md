---
layout: cover
background: ./images/cover-image.jpg
class: text-white
---

# Open API in .NET 9
## August 2024 Update

---

# Schemas and Schema Transformers

<v-click>

````md magic-move
```csharp
JsonNode schema = JsonSchemaExporter
  .GetJsonSchemaAsNode(jsonSerializerOptions, type);
```
```csharp
JsonSchemaExporterOptions jsonSchemaExporterOptions = new()
{
  TransformSchemaNode = (context, schema) =>
  {
    // OpenAPI-specific transformations happen here
  }
};
JsonNode schema = JsonSchemaExporter
  .GetJsonSchemaAsNode(jsonSerializerOptions, type, jsonSchemaExporterOptions);
```
```csharp
JsonSchemaExporterOptions jsonSchemaExporterOptions = new()
{
  TransformSchemaNode = (context, schema) =>
  {
    // OpenAPI-specific transformations happen here
  }
};
JsonNode schema = JsonSchemaExporter
  .GetJsonSchemaAsNode(jsonSerializerOptions, type, jsonSchemaExporterOptions);
OpenApiJsonSchema openApiJsonSchema =
  JsonSerializer.Deserialize(schema, OpenApiJsonSchemaContext.Default.OpenApiJsonSchema);
OpenApiSchema openApiSchema = openApiJsonSchema.Schema;
```
```csharp
JsonSchemaExporterOptions jsonSchemaExporterOptions = new()
{
  TransformSchemaNode = (context, schema) =>
  {
    // OpenAPI-specific transformations happen here
  }
};
JsonNode schema = JsonSchemaExporter
  .GetJsonSchemaAsNode(jsonSerializerOptions, type, jsonSchemaExporterOptions);
OpenApiJsonSchema openApiJsonSchema =
  JsonSerializer.Deserialize(schema, OpenApiJsonSchemaContext.Default.OpenApiJsonSchema);
OpenApiSchema openApiSchema = openApiJsonSchema.Schema;
await ApplySchemaTransformersAsync(openApiSchema, context, cancellationToken);
```
```csharp
JsonSchemaExporterOptions jsonSchemaExporterOptions = new()
{
  TransformSchemaNode = (context, schema) =>
  {
    // OpenAPI-specific transformations happen here
  }
};
JsonNode schema = JsonSchemaExporter
  .GetJsonSchemaAsNode(jsonSerializerOptions, type, jsonSchemaExporterOptions);
OpenApiJsonSchema openApiJsonSchema =
  JsonSerializer.Deserialize(schema, OpenApiJsonSchemaContext.Default.OpenApiJsonSchema);
OpenApiSchema openApiSchema = openApiJsonSchema.Schema;
await ApplySchemaTransformersAsync(openApiSchema, context, cancellationToken);
return openApiSchema;
```
````

</v-click>

---
layout: cover
background: ./images/schema-transition.png
class: text-white
---

many lines of code later...

---

````md magic-move
```csharp
var document = GenerateOpenApiDocument();
```
```csharp
var document = await GenerateOpenApiDocumentAsync();
await ApplySchemaReferenceTransformer(document, context, cancellationToken);
```
```csharp
ApplySchemaReferenceTransformer(document, context, cancellationToken)
{
  foreach (var operation in operations)
  {
    foreach (var parameter in operation.Parameters)
    {
      if (shouldRepresentSchemaByRef(parameter.Schema))
      {
      }
    }
  }
}
```
```csharp
ApplySchemaReferenceTransformer(document, context, cancellationToken)
{
  foreach (var operation in operations)
  {
    foreach (var parameter in operation.Parameters)
    {
      if (shouldRepresentSchemaByRef(parameter.Schema))
      {
        var schemaReferenceId = options.GetSchemaReferenceId();
      }
    }
  }
}
```
```csharp
ApplySchemaReferenceTransformer(document, context, cancellationToken)
{
  foreach (var operation in operations)
  {
    foreach (var parameter in operation.Parameters)
    {
      if (shouldRepresentSchemaByRef(parameter.Schema))
      {
        var schemaReferenceId = options.GetSchemaReferenceId();
        document.Components.Schemas[schemaReferenceId] = parameter.Schema;
      }
    }
  }
}
```
```csharp
ApplySchemaReferenceTransformer(document, context, cancellationToken)
{
  foreach (var operation in operations)
  {
    foreach (var parameter in operation.Parameters)
    {
      if (shouldRepresentSchemaByRef(parameter.Schema))
      {
        var schemaReferenceId = options.GetSchemaReferenceId();
        document.Components.Schemas[schemaReferenceId] = parameter.Schema;
        parameter.Schema = new OpenApiReference { Type = OpenApiReferenceType.Schema, Id =  schemaReferenceId };
      }
    }
  }
}
```
````

---
layout: cover
---

✨ The Implications ✨

---

# Might need to mark more types in your JSON serializer context

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/", (DateOnly date) => ...);

[JsonSerializable(typeof(DateOnly))]
public partial class AppJsonSerializerContext : JsonSerializerContext { }
```

---

# Need to use schema transformers to define schemas for types with custom transformers

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi(options =>
{
  options.AddSchemaTransformer((schema, context, cancellationToken) =>
  {
    if (context.JsonTypeInfo.Type == typeof(MyCustomType))
    {
      schema.Properties["age"] = new OpenApiSchema { Type = "integer" };
      schema.Properties["name"] = new OpenApiSchema { Type = "string" };
      return Task.CompletedTask;
    }
  });
});

var app = builder.Build();

app.MapPost("/", (MyCustomType type) => ..);

public record MyCustomType(int Age, string Name);
public class MyCustomTypeConverter : JsonConverter<MyCustomType> { }
```

---

# Schemas may look a little different

- No `additionalProperties` set on schemas
- No support for `readOnly`/`writeOnly`
- `allOf` for representing multiple parameters bound from a form
- `anyOf` for polymorphic types (based on `[JsonDerivedType]`)
- `[Consumes]` for controllers only respected when input formatters configured correctly
- Schema transformers are your escape hatch

---

# Native AoT Support 🥳

- Supported for minimal APIs
- Trim and native AoT compat for:
  - Microsoft.AspNetCore.OpenApi
  - Microsoft.AspNetCore.Mvc.ApiExplorer
  - Microsoft.OpenApi

---

# XML Doc Support for OpenAPI

```csharp
/// <summary>
/// Create a new <see cref="Todo" /> with the given <paramref name="id" />.
/// </summary>
/// <param name="id">The integer ID associated with the <see cref="Todo" /> to be created</param>  
/// <param name="todo">The <see cref="Todo" /> to insert into the database.</param>
/// <response code="201">A 201 response associated with the inserted <see cref="Todo" />.</response>
/// <response code="404">The todo service could not be found.</response>
public Results<Created<Todo>, NotFound> CreateTodo(int id, Todo todo) { }

/// <summary>
/// Represents a task containing an ID, title, and completion status.
/// </summary>
/// <example>{ id: 1, title: "Buy milk", isCompleted: false }</example>
public record Todo(int id, string Title, bool IsCompleted);

/// <inheritdoc />
public class WorkshopProject : IProject
{
  /// <summary>
  /// The name of the workshop associated with the project.
  /// </summary>
  public required string WorkshopName { get; init; }
} 
```

---
layout: cover
---

# The sad news 😔
## Not shipping in .NET 9 GA

---
layout: cover
---

# The good news 🤠
## Shipping as an experimental package from AspLabs

---

# XML Doc Support Implementation Overview

- Functionality is encompassed in a source generator that does type/comment discovery
- Reusing logic from DocFX and Roslyn as much as possible
- Emits document and schema transformers that documentation to the output document
- Uses interceptors to register generated transformers onto document instances

---

# Acknowledgements

- Eric Erhardt
- Mike Kistler
- Vincent Biret
- Maggie Kimani
- Darrel Miller
- Martin Costello

---