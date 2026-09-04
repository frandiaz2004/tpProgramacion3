using Tp3prog.Entities;

namespace Tp3prog.DAO;

public static class MockDatabase
{
    public static List<Player> Players { get; } = new();
    public static List<Team> Teams { get; } = new();
    public static List<Trainer> Trainers { get; } = new();
    public static List<Student> Students { get; } = new();
    public static List<Course> Courses { get; } = new();
    public static List<Activity> Activities { get; } = new();

    static MockDatabase()
    {
        Players.Add(new Player { Id = 1, Name = "Lionel Perez", Age = 21, Dni = "40111222", Number = 10 });
        Teams.Add(new Team { Id = 1, Name = "Los Halcones", Category = "Primera" });
        Trainers.Add(new Trainer { Id = 1, Name = "Maria Gomez", Age = 35, Dni = "30123456" });
        Students.Add(new Student { Id = 1, Name = "Ana Lopez", Age = 20, Dni = "45123456", File = "A-001" });
        Courses.Add(new Course { Id = 1, Name = "Programacion III" });
        Activities.Add(new Activity
        {
            Id = 1,
            Title = "Practica de API",
            Description = "Crear endpoints CRUD",
            Date = DateTime.Today,
            Type = TypeActivity.Assignment
        });
    }
}