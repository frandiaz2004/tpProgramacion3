using Microsoft.EntityFrameworkCore;
using Tp3prog.DAO.EntityFramework;
using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class UserDAO(AppDbContext context)
{
    public User? GetById(long id) => context.Users.AsNoTracking().SingleOrDefault(user => user.Id == id);

    public User? GetByEmail(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        return context.Users.AsNoTracking().SingleOrDefault(user => user.NormalizedEmail == normalizedEmail);
    }

    public User Create(string email, string passwordHash)
    {
        var user = new User
        {
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            PasswordHash = passwordHash
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    public bool Update(long id, string email, string expectedPasswordHash, string? newPasswordHash)
    {
        var users = context.Users.Where(user => user.Id == id && user.PasswordHash == expectedPasswordHash);
        var trimmedEmail = email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        var changed = newPasswordHash is null
            ? users.ExecuteUpdate(setters => setters
                .SetProperty(user => user.Email, trimmedEmail)
                .SetProperty(user => user.NormalizedEmail, normalizedEmail))
            : users.ExecuteUpdate(setters => setters
                .SetProperty(user => user.Email, trimmedEmail)
                .SetProperty(user => user.NormalizedEmail, normalizedEmail)
                .SetProperty(user => user.PasswordHash, newPasswordHash));
        return changed == 1;
    }

    public bool Delete(long id, string expectedPasswordHash) => context.Users
        .Where(user => user.Id == id && user.PasswordHash == expectedPasswordHash).ExecuteDelete() == 1;

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
