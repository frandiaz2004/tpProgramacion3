using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Tp3prog.Entities;

namespace Tp3prog.Contracts;

public abstract class EntityRequest
{
    [Range(0, long.MaxValue)]
    public long Id { get; set; }
}

public abstract class PersonRequest : EntityRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Age { get; set; }

    [StringLength(20)]
    public string? Dni { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class PlayerRequest : PersonRequest
{
    [Range(1, 99)]
    public int Number { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class StudentRequest : PersonRequest
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string File { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class TrainerRequest : PersonRequest
{
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CourseRequest : EntityRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class TeamRequest : EntityRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Category { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ActivityRequest : EntityRequest
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    public DateTime? Date { get; set; }

    [EnumDataType(typeof(TypeActivity))]
    public TypeActivity Type { get; set; }
}
