using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Tp3prog.Entities;

public class User
{
    public long Id { get; set; }

    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
}
