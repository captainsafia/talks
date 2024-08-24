using GymManager.Models;
using GymManager.Services;

namespace GymManager.Endpoints;

public static partial class IEndpointRouteBuilderExtensions 
{
    public static IEndpointRouteBuilder MapSessions(this IEndpointRouteBuilder builder)
    {
        var sessions = builder.MapGroup("/sessions");
        sessions.MapGet("", (ISessionService service) => service.GetSessions());
        sessions.MapGet("{id}", (ISessionService service, int id) => service.GetSession(id));
        sessions.MapPost("", (ISessionService service, Session session) => service.AddSession(session));
        sessions.MapPut("{id}", (ISessionService service, int id, Session session) => service.UpdateSession(session));
        sessions.MapDelete("{id}", (ISessionService service, int id) => service.DeleteSession(id));
        return builder;
    }
}