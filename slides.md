---
layout: cover
---

# OpenAPI in .NET
## Past, Present, and Future

---

# A look behind the code...

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello world!");

app.Run();
```

---

# ...into the implementation...

```mermaid
flowchart LR
	mvc[Controller-based API]
	minapi[Minimal API]
	defdescprov[DefaultApiDescriptionProvider]
	endpdescprov[EndpointMetadataDescriptionProvider]
	desccoll[IApiDescriptionCollection]
	compservice[[OpenApiComponentService]]
	docsvc[[OpenApiDocumentService]]
	idocprovider[[IDocumentProvider]]
	meas[Microsoft.Extensions.ApiDescription.Server]
	mvc --> defdescprov 
	mvc --> endpdescprov
	minapi --> endpdescprov
	defdescprov --> desccoll
	endpdescprov --> desccoll
	desccoll --> docsvc
	idocprovider --> docsvc
	meas --> idocprovider
	compservice --> docsvc
```

---

# ...and the history...

---

# ...and the future!

---