using GymManager.Models;

namespace GymManager.Services;

public interface ITrainerService
{
    Trainer AddTrainer(Trainer trainer);
    Trainer UpdateTrainer(Trainer trainer);
    Trainer GetTrainer(int id);
    IEnumerable<Trainer> GetTrainers();
    void DeleteTrainer(int id);
}