namespace GymManager.Models;

public class Trainer
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public TrainerLevel TrainerLevel { get; set; }
}