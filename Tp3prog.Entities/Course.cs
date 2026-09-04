using System.ComponentModel.DataAnnotations;

namespace Tp3prog.Entities;

public class Course
{
    public long Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    public List<Student> Students { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}