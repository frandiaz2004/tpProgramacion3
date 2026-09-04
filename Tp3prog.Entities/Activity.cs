using System.ComponentModel.DataAnnotations;

namespace Tp3prog.Entities;

public class Activity
{
    public long Id { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public TypeActivity Type { get; set; }
}