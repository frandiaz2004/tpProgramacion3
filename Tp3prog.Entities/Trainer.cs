namespace Tp3prog.Entities;

public class Trainer : Person
{
    public List<Team> Teams { get; set; } = new();
}