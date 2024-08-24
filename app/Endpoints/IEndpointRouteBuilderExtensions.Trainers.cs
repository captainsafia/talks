using GymManager.Models;
using GymManager.Services;

namespace GymManager.Endpoints;

public static partial class IEndpointRouteBuilderExtensions 
{
    public static IEndpointRouteBuilder MapTrainers(this IEndpointRouteBuilder builder)
    {
        var trainers = builder.MapGroup("/trainers");
        trainers.MapGet("", (ITrainerService service) => service.GetTrainers());
        trainers.MapGet("{id}", (ITrainerService service, int id) => service.GetTrainer(id));
        trainers.MapPost("", (ITrainerService service, Trainer trainer) => service.AddTrainer(trainer));
        trainers.MapPut("{id}", (ITrainerService service, int id, Trainer trainer) => service.UpdateTrainer(trainer));
        trainers.MapDelete("{id}", (ITrainerService service, int id) => service.DeleteTrainer(id));
        return builder;
    }
}