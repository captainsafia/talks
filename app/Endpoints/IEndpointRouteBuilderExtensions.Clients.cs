using GymManager.Models;
using GymManager.Services;

namespace GymManager.Endpoints;

public static partial class IEndpointRouteBuilderExtensions 
{
    public static IEndpointRouteBuilder MapClients(this IEndpointRouteBuilder builder)
    {
        var clients = builder.MapGroup("/clients");
        clients.MapGet("", (IClientService service) => service.GetClients());
        clients.MapGet("{id}", (IClientService service, int id) => service.GetClient(id));
        clients.MapPost("", (IClientService service, Client client) => service.AddClient(client));
        clients.MapPut("{id}", (IClientService service, int id, Client client) => service.UpdateClient(client));
        clients.MapDelete("{id}", (IClientService service, int id) => service.DeleteClient(id));
        return builder;
    }
}