using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.DAO;

internal static class MembershipLocks
{
    public static IDbContextTransaction Begin(AppDbContext context) => context.Database.BeginTransaction(
        context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted);

    // Los UPDATE sin cambios toman bloqueos de escritura hasta el commit.
    // Todas las operaciones toman primero personas y después padres, por ID.
    public static void People(AppDbContext context, IEnumerable<long> ids)
    {
        foreach (var id in ids.Distinct().Order())
            context.Persons.Where(person => person.Id == id)
                .ExecuteUpdate(setters => setters.SetProperty(person => person.Age, person => person.Age));
    }

    public static void Teams(AppDbContext context, IEnumerable<long> ids)
    {
        foreach (var id in ids.Distinct().Order())
            context.Teams.Where(team => team.Id == id)
                .ExecuteUpdate(setters => setters.SetProperty(team => team.Name, team => team.Name));
    }

    public static void Courses(AppDbContext context, IEnumerable<long> ids)
    {
        foreach (var id in ids.Distinct().Order())
            context.Courses.Where(course => course.Id == id)
                .ExecuteUpdate(setters => setters.SetProperty(course => course.Name, course => course.Name));
    }
}
