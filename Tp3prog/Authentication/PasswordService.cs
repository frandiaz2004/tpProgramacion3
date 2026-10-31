namespace Tp3prog.Authentication;

public sealed class PasswordService
{
    private const int WorkFactor = 12;
    private readonly string dummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), WorkFactor);

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string? passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash ?? dummyHash);
}
