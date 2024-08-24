using GymManager.Models;
using Microsoft.EntityFrameworkCore;

public class GymManagerContext(DbContextOptions<GymManagerContext> contextOptions) : DbContext(contextOptions)
{
    public DbSet<Gym> Gyms { get; set; }
    public DbSet<Trainer> Trainers { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Session> Sessions { get; set; }

}