using System.ComponentModel.DataAnnotations;

namespace Tp3prog.Entities;

public class Student : Person
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string File { get; set; } = string.Empty;
    public List<Course> Courses { get; set; } = new();
}