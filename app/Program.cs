using GymManager.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GymManagerContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GymManagerContext")));

var app = builder.Build();

app.MapClients()
    .MapGyms()
    .MapTrainers()
    .MapSessions();

app.Run();
