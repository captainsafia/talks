namespace GymManager.Models;

public class Session
{
    public int Id { get; set; }
    public required Trainer Trainer { get; set; }
    public required Client Client { get; set; }
    public required Gym Gym { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Notes { get; set; }
}