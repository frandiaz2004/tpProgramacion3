using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Tp3prog.Contracts;

public abstract class EmailDTO
{
    private string email = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email
    {
        get => email;
        set => email = value?.Trim() ?? string.Empty;
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateUserDTO : EmailDTO
{
    [Required]
    [MinLength(8)]
    [PasswordByteLength]
    public string Password { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LoginDTO : EmailDTO
{
    [Required]
    [PasswordByteLength]
    public string Password { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateUserDTO : EmailDTO, IValidatableObject
{
    [Required]
    [PasswordByteLength]
    public string CurrentPassword { get; set; } = string.Empty;

    [MinLength(8)]
    [PasswordByteLength]
    public string? NewPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NewPassword is not null && string.IsNullOrWhiteSpace(NewPassword))
            yield return new ValidationResult("La nueva contraseña no puede estar vacía.", [nameof(NewPassword)]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class DeleteUserDTO
{
    [Required]
    [PasswordByteLength]
    public string CurrentPassword { get; set; } = string.Empty;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class PasswordByteLengthAttribute : ValidationAttribute
{
    public PasswordByteLengthAttribute() : base("La contraseña no puede superar los 72 bytes en UTF-8.") { }

    public override bool IsValid(object? value) => value is null || value is string password && Encoding.UTF8.GetByteCount(password) <= 72;
}

public record UserResponse(long Id, string Email);
public record LoginResponse(string Token, DateTime ExpiresAt);
