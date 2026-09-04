using System.ComponentModel.DataAnnotations;

namespace Tp3prog.Entities;

public class Player : Person
{
    [Range(1, 99)]
    public int Number { get; set; }
    public List<Team> Teams { get; set; } = new();
}