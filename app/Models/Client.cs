namespace GymManager.Models;

public class Client
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public required string Email { get; set; }
    public DateOnly RegistrationDate { get; set; }
    public SubscriptionLevel SubscriptionLevel { get; set; }
}