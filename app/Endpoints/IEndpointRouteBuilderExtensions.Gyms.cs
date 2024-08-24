using GymManager.Models;

namespace GymManager.Endpoints;

public static partial class IEndpointRouteBuilderExtensions 
{
    public static IEndpointRouteBuilder MapGyms(this IEndpointRouteBuilder builder)
    {
        var gyms = builder.MapGroup("/gyms");
        gyms.MapGet("/", (IGymService service) => service.GetGyms());
        gyms.MapGet("/{id}", (IGymService service, int id) => service.GetGym(id));
        gyms.MapPost("/", (IGymService service, Gym gym) => service.AddGym(gym));
        return builder;
    }
}