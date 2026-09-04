using System.ComponentModel.DataAnnotations;

namespace Tp3prog.Entities;

public class Team
{
    public long Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Category { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = new();
    public List<Trainer> Trainers { get; set; } = new();
}