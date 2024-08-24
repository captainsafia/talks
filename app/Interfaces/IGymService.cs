using GymManager.Models;

public interface IGymService
{
    Gym AddGym(Gym gym);
    Gym UpdateGym(Gym gym);
    Gym GetGym(int id);
    IEnumerable<Gym> GetGyms();
    void DeleteGym(int id);
}