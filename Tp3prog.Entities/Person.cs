using System.ComponentModel.DataAnnotations;

namespace Tp3prog.Entities;

public class Person
{
    public long Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Age { get; set; }

    [StringLength(20)]
    public string? Dni { get; set; }
}