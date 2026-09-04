using Microsoft.EntityFrameworkCore;
using Tp3prog.Entities;

namespace Tp3prog.DAO.EntityFramework;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Activity> Activities => Set<Activity>();
}